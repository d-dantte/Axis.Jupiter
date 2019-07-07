using System;
using System.Linq;
using Axis.Jupiter.Contracts;
using Axis.Jupiter.Helpers;
using Axis.Luna.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Axis.Jupiter.EFCore
{
    public interface IEFStoreQuery: IStoreQuery
    {
        DbContext EFContext { get; }
    }

    public class EFStoreQuery: IEFStoreQuery
    {
        private readonly DbContext _context;

        public DbContext EFContext => _context;


        public EFStoreQuery(DbContext context)
        {
            _context = context ?? throw new Exception("Invalid Context specified: null");
        }

        public IQueryable<Entity> Query<Entity>(params IPropertyPath[] paths)
        where Entity : class
        {
            var query = _context
                .Set<Entity>()
                .As<IQueryable<Entity>>();

            return paths.Aggregate(query, (current, path) => query.Include(Flatten(path)));
        }

        public IQueryable<Entity> Query<Entity>(params Func<IPropertyPath<Entity, Entity>, IPropertyPath>[] pathGenerators)
        where Entity : class
        {
            var query = _context
                .Set<Entity>()
                .As<IQueryable<Entity>>();

            return pathGenerators.Aggregate(query, (current, pathGenerator) =>
            {
                var origin = Paths.From<Entity>();
                return query.Include(Flatten(pathGenerator.Invoke(origin)));
            });
        }

        private  static string Flatten(IPropertyPath path)
        {
            if (path.IsOrigin)
                return "";

            else if (path.Parent.IsOrigin)
                return path.Property;

            else
                return $"{Flatten(path.Parent)}.{path.Property}";
        }
    }
}
