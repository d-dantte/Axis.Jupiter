using Axis.Luna.Extensions;
using System;
using System.Collections.Generic;

namespace Axis.Jupiter.Kore
{
    public class PersistenceProvider
    {
        private IDataContext _context = null;

        public Registrar OperationCache { get; } = new Registrar();

        public PersistenceProvider(IDataContext context, Action<Registrar> operationRegistration  = null)
        {
            operationRegistration?.Invoke(OperationCache);

            _context = context.ThrowIfNull("invalid context supplied");
        }

        public bool CanBulkInsert<Entity>() => OperationCache.BulkInsertOperations.ContainsKey(typeof(Entity));
        public bool CanInsert<Entity>() => OperationCache.InsertOperations.ContainsKey(typeof(Entity));
        public bool CanUpdate<Entity>() => OperationCache.UpdateOperations.ContainsKey(typeof(Entity));
        public bool CanDelete<Entity>() => OperationCache.DeleteOperations.ContainsKey(typeof(Entity));


        public IEnumerable<Entity> BulkInsert<Entity>(IEnumerable<Entity> darr)
        => ((Func<IEnumerable<Entity>, IDataContext, IEnumerable<Entity>>)OperationCache.BulkInsertOperations[typeof(Entity)]).Invoke(darr, _context);

        public Entity Insert<Entity>(Entity d)
        => ((Func<Entity, IDataContext, Entity>)OperationCache.InsertOperations[typeof(Entity)]).Invoke(d, _context);

        public Entity Update<Entity>(Entity d)
        => ((Func<Entity, IDataContext, Entity>)OperationCache.UpdateOperations[typeof(Entity)]).Invoke(d, _context);

        public Entity Delete<Entity>(Entity d)
        => ((Func<Entity, IDataContext, Entity>)OperationCache.DeleteOperations[typeof(Entity)]).Invoke(d, _context);


        
        public class Registrar
        {
            internal Dictionary<Type, dynamic> BulkInsertOperations = new Dictionary<Type, dynamic>();
            internal Dictionary<Type, dynamic> InsertOperations = new Dictionary<Type, dynamic>();
            internal Dictionary<Type, dynamic> UpdateOperations = new Dictionary<Type, dynamic>();
            internal Dictionary<Type, dynamic> DeleteOperations = new Dictionary<Type, dynamic>();

            public Registrar RegisterBulkInsert<Entity>(Func<IEnumerable<Entity>, IDataContext, IEnumerable<Entity>> inserter)
            {
                InsertOperations[typeof(Entity)] = inserter.ThrowIfNull();
                return this;
            }
            public Registrar RegisterInsert<Entity>(Func<Entity, IDataContext, Entity> inserter)
            {
                InsertOperations[typeof(Entity)] = inserter.ThrowIfNull();
                return this;
            }
            public Registrar RegisterUpdate<Entity>(Func<Entity, IDataContext, Entity> updater)
            {
                UpdateOperations[typeof(Entity)] = updater.ThrowIfNull();
                return this;
            }
            public Registrar RegisterDelete<Entity>(Func<Entity, IDataContext, Entity> deleter)
            {
                DeleteOperations[typeof(Entity)] = deleter.ThrowIfNull();
                return this;
            }
        }
    }
}
