using System;
using System.Linq;
using System.Linq.Expressions;
using Axis.Jupiter.Contracts;
using Axis.Luna.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Axis.Jupiter.EFCore
{
    public class EFStoreQuery: IStoreQuery
    {
        private readonly DbContext _context;
        

        public EFStoreQuery(DbContext context)
        {
            _context = context ?? throw new Exception("Invalid Context specified: null");
        }


        public IQueryable<Entity> Query<Entity>(params Expression<Func<Entity, object>>[] entityPropertyPaths)
        where Entity: class
        {
            var query = _context
                .Set<Entity>()
                .As<IQueryable<Entity>>();

            return entityPropertyPaths.Aggregate(query, (current, path) => current.Include(path));
        }

    }
}
