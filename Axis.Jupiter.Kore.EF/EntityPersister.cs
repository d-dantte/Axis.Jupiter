using static Axis.Luna.Extensions.ExceptionExtensions;

using Axis.Jupiter.Kore.Commands;
using System;
using System.Collections.Generic;
using Axis.Luna.Operation;
using Axis.Jupiter.Europa;
using System.Data.SqlClient;
using Axis.Luna.Extensions;
using System.Data.Entity;
using System.Linq;

namespace Axis.Jupiter.Kore.EF
{
    public class EntityPersister : IPersistenceCommands
    {
        private DataStore _dataStore;

        public EntityPersister(DataStore dataStore)
        {
            ThrowNullArguments(() => dataStore);

            _dataStore = dataStore;
        }

        #region Add
        public IOperation<Entity> Add<Entity>(Entity d)
        where Entity : class => LazyOp.Try(() =>
        {
            var entity =_dataStore.Set<Entity>().Add(d);
            _dataStore.SaveChanges();
            return entity;
        });

        public IOperation<IEnumerable<Entity>> AddBatch<Entity>(IEnumerable<Entity> entities)
        where Entity : class => LazyOp.Try(() =>
        {
            _dataStore.InsertBatch(SqlBulkCopyOptions.Default, entities).Execute();
            return entities;
        });
        #endregion

        #region Delete
        public IOperation<Entity> Delete<Entity>(Entity d)
        where Entity : class => LazyOp.Try(() =>
        {
            d = _dataStore.Set<Entity>().Remove(d);
            _dataStore.SaveChanges();
            return d;
        });

        public IOperation<IEnumerable<Entity>> DeleteBatch<Entity>(IEnumerable<Entity> entities)
        where Entity : class => LazyOp.Try(() =>
        {
            var deletedEntities =_dataStore.Set<Entity>().RemoveRange(entities);
            _dataStore.SaveChanges();
            return deletedEntities;
        });
        #endregion

        #region Update
        public IOperation<Entity> Update<Entity>(Entity entity, Action<Entity> copyFunction = null)
        where Entity : class => UpdateEntity(entity, copyFunction).Then(_e =>
        {
            _dataStore.SaveChanges();
            return _e;
        });

        public IOperation<IEnumerable<Entity>> UpdateBatch<Entity>(IEnumerable<Entity> sequence, Action<Entity> copyFunction = null)
        where Entity : class => LazyOp.Try(() =>
        {
            return sequence
                .Select(_entity => UpdateEntity(_entity, copyFunction).Resolve())
                .ToList() //forces evaluation of the IEnumerable
                .UsingValue(_list =>  _dataStore.SaveChanges());
        });

        private IOperation<Entity> UpdateEntity<Entity>(Entity entity, Action<Entity> copyFunction)
        where Entity : class => LazyOp.Try(() =>
        {
            //get the object keys. This CAN be cached, but SHOULD it be?
            var keys = _dataStore
                .MappingFor<Entity>()
                .Properties
                .Where(_p => _p.IsKey)
                .Select(_p => _p.ClrProperty.Name.ValuePair(entity.PropertyValue(_p.ClrProperty.Name)));

            var set = _dataStore.Set<Entity>();

            //find the entity locally
            var local = set.Local.FirstOrDefault(_e =>
            {
                return keys.Select(_k => _e.PropertyValue(_k.Key))
                           .SequenceEqual(keys.Select(_k => _k.Value));
            });

            //if the entity was found locally, apply the copy function or copy from the supplied object
            if (local != null)
            {
                if (copyFunction == null) local.CopyFrom(entity);
                else copyFunction.Invoke(local);

                entity = local;
            }

            //if the entity wasn't found locally, simply attach it
            else set.Attach(entity);

            _dataStore.Entry(entity).State = EntityState.Modified;
            return entity;
        });
        #endregion

        
        public void Dispose()
        {
            _dataStore.Dispose();
        }
    }
}
