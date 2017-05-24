using System;
using System.Linq;
using System.Linq.Expressions;

namespace Axis.Jupiter.Kore.Commands
{
    public interface IQueryCommands
    {
        IQueryable<Entity> Query<Entity>(params Expression<Func<Entity, object>>[] includes) where Entity: class;
    }
}
