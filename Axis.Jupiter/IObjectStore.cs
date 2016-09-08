using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Axis.Jupiter
{

    public interface IObjectStore<Entity>: IObjectFactory<Entity>
    {

        IDataContext Context { get; }
        IObjectMetadata Metadata(Entity dobj);

        Entity LoadReferences<TProp>(Entity dobj, params Expression<Func<Entity, TProp>>[] tprops)
        where TProp : class;

        string StoreName { get; }

        IQueryable<Entity> Query { get; }
        IQueryable<Entity> ReadonlyQuery { get; }

        IQueryable<Entity> QueryWith<TProp>(params Expression<Func<Entity, TProp>>[] tprops);

        /// <summary>
        /// Add an object to the store. Added objects do not represent objects that are already persisted in the database
        /// </summary>
        /// <param name="dobj"></param>
        /// <returns></returns>
        IObjectStore<Entity> Add(Entity dobj);
        IObjectStore<Entity> Add(IEnumerable<Entity> dobjs);

        /// <summary>
        /// This method assumes that the object is already persisted, hence it prepares the object so when the next call to "Commit" comes,
        /// the object is sent to the back end.
        /// </summary>
        /// <param name="dobj"></param>
        /// <returns></returns>
        IObjectStore<Entity> Modify(Entity dobj);
        IObjectStore<Entity> Modify(IEnumerable<Entity> dobjs);
        IObjectStore<Entity> Modify(Entity dobj, bool commit);
        IObjectStore<Entity> Modify(IEnumerable<Entity> dobjs, bool commit);

        //Entity newInstance();
        //Entity newInstance(Action<Entity> initializer);

        /// <summary>
        /// Should search for an object, first in any local caches if available, before external sources.
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        Entity Find(params object[] keys);
        Task<Entity> FindAsync(params object[] arguments);

        /// <summary>
        /// Should search for an object, first in any local caches if available, before external sources.
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        Entity Find(Expression<Func<Entity, bool>> predicate);

        IObjectStore<Entity> Delete(Entity dobj);
        IObjectStore<Entity> Delete(IEnumerable<Entity> dobjs);
        IObjectStore<Entity> Delete(Entity dobj, bool commit);
        IObjectStore<Entity> Delete(IEnumerable<Entity> dobjs, bool commit);
    }
}
