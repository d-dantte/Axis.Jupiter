using System;
using System.Linq;
using System.Linq.Expressions;

namespace Axis.Jupiter.Query
{
    public interface IEntityQuery
    {
        IQueryable<Entity> Query<Entity>(params Expression<Func<Entity, object>>[] includes) where Entity: class;
    }
}
