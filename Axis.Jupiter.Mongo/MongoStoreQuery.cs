using System;
using System.Linq;
using System.Linq.Expressions;
using Axis.Jupiter.Contracts;
using MongoDB.Driver;

namespace Axis.Jupiter.Mongo
{
    public class MongoStoreQuery: IStoreQuery
    {
        private readonly MongoClient _client;
        private readonly string _databaseLabel;
        private readonly MongoDatabaseSettings _settings;
        

        public MongoStoreQuery(
            string databaseLabel, 
            MongoClient client, 
            MongoDatabaseSettings settings = null)
        {
            _databaseLabel = string.IsNullOrWhiteSpace(databaseLabel)
                ? throw new Exception("Invalid Database label specified")
                : databaseLabel;

            _client = client ?? throw new Exception("Invalid Client specified: null");
            _settings = settings;
        }

        public IQueryable<Entity> Query<Entity>(params Expression<Func<Entity, object>>[] entityPropertyPaths) 
        where Entity : class
        {
            //property paths are ignored because document dbs pull in the entire object graph based on the filter specified

            return _client
                .GetDatabase(_databaseLabel, _settings)
                .GetCollection<Entity>(typeof(Entity).FullName)
                .AsQueryable();
        }
    }
}
