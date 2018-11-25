using System;
using System.Collections.Generic;
using System.Text;
using Axis.Jupiter.Contracts;
using Axis.Luna.Operation;
using MongoDB.Driver;

namespace Axis.Jupiter.Mongo
{
    public class MongoStoreCommand: IStoreCommand
    {
        private readonly MongoClient _client;
        private readonly string _databaseLabel;
        private readonly MongoDatabaseSettings _settings;

        public string StoreName { get; }


        public MongoStoreCommand(string storeName, string databaseLabel, MongoClient client, MongoDatabaseSettings settings = null)
        {
            StoreName = string.IsNullOrWhiteSpace(storeName)
                ? throw new Exception("Invalid Store Name specified")
                : storeName;

            _databaseLabel = string.IsNullOrWhiteSpace(databaseLabel)
                ? throw new Exception("Invalid Database label specified")
                : databaseLabel;

            _client = client ?? throw new Exception("Invalid Client specified: null");
            _settings = settings;
        }

        public Operation<Model> Add<Model>(Model d) where Model : class
        {
            throw new NotImplementedException();
        }

        public Operation AddBatch<Model>(IEnumerable<Model> d) where Model : class
        {
            throw new NotImplementedException();
        }

        public Operation<Model> Update<Model>(Model d) where Model : class
        {
            throw new NotImplementedException();
        }

        public Operation UpdateBatch<Model>(IEnumerable<Model> d) where Model : class
        {
            throw new NotImplementedException();
        }

        public Operation<Model> Delete<Model>(Model d) where Model : class
        {
            throw new NotImplementedException();
        }

        public Operation DeleteBatch<Model>(IEnumerable<Model> d) where Model : class
        {
            throw new NotImplementedException();
        }
    }
}
