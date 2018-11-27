using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Axis.Jupiter.Contracts;
using Axis.Luna.Extensions;
using Axis.Luna.Operation;
using MongoDB.Driver;

namespace Axis.Jupiter.Mongo
{
    public class MongoStoreCommand: IStoreCommand
    {
        private readonly MongoClient _client;
        private readonly string _databaseLabel;
        private readonly MongoDatabaseSettings _settings;
        private readonly EntityConfiguration _entityConfiguration;

        #region Fast Call enablers

        private readonly MethodInfo _addToCollectionMethod;
        private readonly MethodInfo _addRangeToCollectionMethod;
        private readonly MethodInfo _updateInCollectionMethod;
        private readonly MethodInfo _updateRangeInCollectionMethod;
        private readonly MethodInfo _removeFromCollectionMethod;
        private readonly MethodInfo _removeRangeFromCollectionMethod;

        #endregion

        public string StoreName { get; }


        public MongoStoreCommand(string storeName, string databaseLabel, 
                                 EntityConfiguration entityConfiguration,
                                 MongoClient client, MongoDatabaseSettings settings = null)
        {
            StoreName = string.IsNullOrWhiteSpace(storeName)
                ? throw new Exception("Invalid Store Name specified")
                : storeName;

            _databaseLabel = string.IsNullOrWhiteSpace(databaseLabel)
                ? throw new Exception("Invalid Database label specified")
                : databaseLabel;

            _client = client ?? throw new Exception("Invalid Client specified: null");
            _entityConfiguration = entityConfiguration ?? throw new Exception("Invalid Model Transformer specified: null");
            _settings = settings;

            _addToCollectionMethod = GetType().GetMethod(nameof(AddToCollection));
            _addRangeToCollectionMethod = GetType().GetMethod(nameof(AddRangeToCollection));
            _updateInCollectionMethod = GetType().GetMethod(nameof(UpdateInCollection));
        }

        public Operation<Model> Add<Model>(Model model) 
        where Model : class => Operation.Try(async () =>
        {
            var transformMap = _entityConfiguration
                .ModelTransformer.Transforms()
                .FirstOrDefault(map => map.ModelType == typeof(Model))
                .ThrowIfNull($"No Transform Map found for model type: {typeof(Model).FullName}");

            //use fast-call to call the AddToCollection<Entity> method on the database object
            var method = _addToCollectionMethod.MakeGenericMethod(transformMap.EntityType);
            var entity = _entityConfiguration.ModelTransformer.ToEntity<Model>(model);
            await this.CallFunc<Task>(method, entity);
            return _entityConfiguration.ModelTransformer.ToModel<Model>(entity);
        });

        private async Task AddToCollection<Entity>(Entity entity)
        {
            await _client
                .GetDatabase(_databaseLabel, _settings)
                .GetCollection<Entity>(typeof(Entity).FullName)
                .InsertOneAsync(entity);
        }
        

        public Operation AddBatch<Model>(IEnumerable<Model> models)
        where Model : class => Operation.Try(async () =>
        {
            var transformMap = _entityConfiguration
                .ModelTransformer.Transforms()
                .FirstOrDefault(map => map.ModelType == typeof(Model))
                .ThrowIfNull($"No Transform Map found for model type: {typeof(Model).FullName}");

            //use fast-call to call the AddToCollection<Entity> method on the database object
            var method = _addRangeToCollectionMethod.MakeGenericMethod(transformMap.EntityType);
            var entities = models.Select(_entityConfiguration.ModelTransformer.ToEntity);
            await this.CallFunc<Task>(method, entities.ToArray() as object); //so we take the entire array as one parameter into the method
        });

        private async Task AddRangeToCollection<Entity>(Entity[] entity)
        {
            await _client
                .GetDatabase(_databaseLabel, _settings)
                .GetCollection<Entity>(typeof(Entity).FullName)
                .InsertManyAsync(entity);
        }


        public Operation<Model> Update<Model>(Model model)
        where Model : class => Operation.Try(async () =>
        {
            var transformMap = _entityConfiguration
                .ModelTransformer.Transforms()
                .FirstOrDefault(map => map.ModelType == typeof(Model))
                .ThrowIfNull($"No Transform Map found for model type: {typeof(Model).FullName}");

            //use fast-call to call the AddToCollection<Entity> method on the database object
            var method = _addToCollectionMethod.MakeGenericMethod(transformMap.EntityType);
            var entity = _entityConfiguration.ModelTransformer.ToEntity(model);
            await this.CallFunc<Task>(method, entity);
            return _entityConfiguration.ModelTransformer.ToModel<Model>(entity);
        });

        public async Task UpdateInCollection<Entity>(Entity entity)
        {
            var info = _entityConfiguration
                .EntityInfoMap
                .InfoFor<Entity>();

            var keyValues = info
                .KeyProperties
                .Select(propName => entity.PropertyValue(propName))
                .ToArray();

            var filter = info.IdentityFilter<Entity>(keyValues);

            await _client
                .GetDatabase(_databaseLabel, _settings)
                .GetCollection<Entity>(typeof(Entity).FullName)
                .ReplaceOneAsync(filter, entity);
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
