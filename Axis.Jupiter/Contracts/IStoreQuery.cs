using Axis.Jupiter.Helpers;
using System;
using System.Linq;

namespace Axis.Jupiter.Contracts
{
    public interface IStoreQuery
    {
        //IQueryable<Entity> Query<Entity>(params IPropertyPath[] paths)
        //where Entity : class;

        IQueryable<Entity> Query<Entity>(params Func<IPropertyPath<Entity, Entity>, IPropertyPath>[] pathGenerators)
        where Entity : class;
    }
}
