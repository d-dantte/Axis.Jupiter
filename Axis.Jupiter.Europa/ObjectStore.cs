using static Axis.Luna.Extensions.ObjectExtensions;
using static Axis.Luna.Extensions.TypeExtensions;
using static Axis.Luna.Extensions.EnumerableExtensions;

using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections;

namespace Axis.Jupiter.Europa
{
    public class ObjectStore<Entity>: IObjectStore<Entity> 
    where Entity: class
    {
        public DbSet<Entity> Set { get; private set; }

        public string StoreName { get; private set; }

        public int TransactionBatchCount { get; private set; }

        public IDataContext Context => _context;
        internal EuropaContext _context { get; private set; }

        public IQueryable<Entity> ReadonlyQuery => Set.AsNoTracking();

        public IQueryable<Entity> Query
            => (_context.QueryGeneratorFor<Entity>() as Func<IDataContext, IQueryable<Entity>>)?.Invoke(_context) ?? Set;

        public IQueryable<Entity> QueryWith<TProp>(params Expression<Func<Entity, TProp>>[] tprops)
            => tprops.Aggregate(Query, (_q, _tprop) => _q.Include(_tprop));



        public ObjectStore(EuropaContext dstore): this(typeof(Entity).Name, dstore)
        { }
        public ObjectStore(string storeName, EuropaContext dstore):this(storeName, 1000, dstore)
        { }
        public ObjectStore(string storeName, int batchCount, EuropaContext dstore)
        {
            this.Set = dstore.Set<Entity>();
            _context = dstore;
            this.StoreName = storeName;
        }

        public IObjectStore<Entity> Add(Entity dobj) => Add(dobj.Enumerate());

        public IObjectStore<Entity> Add(IEnumerable<Entity> dobjs)
            => this.UsingValue(@this => (dobjs ?? new Entity[0]).ForAll((cnt, dobj) => Set.Add(dobj)));

        public IObjectStore<Entity> Modify(Entity dobj) => Modify(dobj, false);
        public IObjectStore<Entity> Modify(Entity dobj, bool commit)
        {
            Modify(dobj.Enumerate());
            if (commit) Context.CommitChanges();
            return this;
        }

        public IObjectStore<Entity> Modify(IEnumerable<Entity> dobjs) => Modify(dobjs, false);
        public IObjectStore<Entity> Modify(IEnumerable<Entity> dobjs, bool commit)
        {
            dobjs.ForAll((cnt, next) =>
            {
                var entry = _context.Entry(next);
                if (entry.State == EntityState.Detached) //Add(next); //<- old
                    Set.Attach(next);

                entry.State = EntityState.Modified;
            });

            if (commit) Context.CommitChanges();
            return this;
        }

        public Entity Find(params object[] arguments) => Set.Find(arguments);

        public Task<Entity> FindAsync(params object[] arguments) => Set.FindAsync(arguments);


        public Entity NewObject() => Set.Create();

        public IObjectMetadata Metadata(Entity dobj) => null;

        public Entity LoadReferences<TProp>(Entity dobj, params Expression<Func<Entity, TProp>>[] tprops)
        where TProp : class
        {
            tprops.ToList().ForEach(tprop =>
            {
                if (IsCollectionProperty(tprop))
                {
                    var telt = GetCollectionElement(typeof(TProp));
                    //_context.Entry(dobj).Call("Collection", new[] { telt }, tprop).Call("Load");
                    dynamic d = _context.Entry(dobj);
                    d.Collection(tprop).Load();
                }
                else if (isNavigationProperty(tprop)) _context.Entry(dobj).Reference(tprop).Load();
            });
            return dobj;
        }

        public IObjectStore<Entity> Delete(IEnumerable<Entity> dobjs, bool commit )
        {
            dobjs.ForAll((cnt, dobj) =>
            {
                var entry = _context.Entry(dobj);
                if (entry.State == EntityState.Detached) //Add(dobj); //<- old
                    Set.Attach(dobj);

                Set.Remove(dobj);
            });

            if (commit) _context.SaveChanges();
            return this;
        }
        public IObjectStore<Entity> Delete(IEnumerable<Entity> dobjs) => Delete(dobjs, false);

        public IObjectStore<Entity> Delete(Entity dobj, bool commit) => Delete(dobj.Enumerate(), commit);
        public IObjectStore<Entity> Delete(Entity dobj) => Delete(dobj.Enumerate(), false);

        public Entity Find(Expression<Func<Entity, bool>> predicate)
            => this.Set.Local.FirstOrDefault(predicate.Compile()) ??
               Query.FirstOrDefault(predicate);


        private static Type GetCollectionElement(Type collectionType)
            => collectionType.TypeLineage()
                             .Where(t => t.IsGenericType)
                             .Where(t => t.GetGenericTypeDefinition() == typeof(ICollection<>))
                             .FirstOrDefault()?
                             .GetGenericArguments()[0];
        private static bool IsCollectionProperty<TProp>(Expression<Func<Entity, TProp>> tprop)
            => typeof(TProp).TypeLineage().Any(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(ICollection<>));
        private static bool isNavigationProperty<TProp>(Expression<Func<Entity, TProp>> tprop)
            => (!typeof(TProp).TypeLineage().Any(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(ICollection<>)));
        
    }
}
