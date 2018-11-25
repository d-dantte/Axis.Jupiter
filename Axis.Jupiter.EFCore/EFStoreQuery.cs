using System;
using System.Linq;
using System.Linq.Expressions;
using Axis.Luna.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Axis.Jupiter.EFCore
{
    public class EFStoreQuery: IStoreQuery
    {
        private readonly DbContext _context;

        public string StoreName { get; }


        public EFStoreQuery(string storeName, DbContext context)
        {
            StoreName = string.IsNullOrWhiteSpace(storeName)
                ? throw new Exception("Invalid Store Name specified")
                : storeName;

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
