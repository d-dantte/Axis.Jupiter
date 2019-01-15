using System;
using System.Linq;
using System.Linq.Expressions;

namespace Axis.Jupiter.Contracts
{
    public interface IStoreQuery
    {
        IQueryable<Entity> Query<Entity>(params Expression<Func<Entity, object>>[] entityPropertyPaths)
            where Entity : class;
    }
}
