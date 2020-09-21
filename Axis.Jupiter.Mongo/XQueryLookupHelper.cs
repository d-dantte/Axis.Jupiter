using Axis.Jupiter.MongoDb.XModels;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Axis.Jupiter.MongoDb
{
    public static class XQueryLookupHelper
    {
        public static IEnumerable<TSource> LookupRef<TSource, TRef, TSourceKey, TRefKey>(
            this IQueryable<TSource> sources,
            Expression<Func<TSource, TRef>> @ref,
            MongoClient client)
            where TSource : IMongoEntity<TSourceKey>
            where TRef : IMongoEntity<TRefKey>
        {
            var refCollection = client
            var results = 
                from source in sources
                join @ref in client.GetDatabase("").GetCollection("").AsQueryable()
        }
    }
}
