using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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


        public MongoStoreCommand(
            string databaseLabel,
            EntityConfiguration entityConfiguration,
            MongoClient client, 
            MongoDatabaseSettings settings = null)
        {
            _databaseLabel = string.IsNullOrWhiteSpace(databaseLabel)
                ? throw new ArgumentException("Invalid Database label specified")
                : databaseLabel;

            _client = client ?? throw new ArgumentException("Invalid Client specified: null");
            _entityConfiguration = entityConfiguration ?? throw new Exception("Invalid Model Transformer specified: null");
            _settings = settings;

            _addToCollectionMethod = GetType().GetMethod(nameof(AddToCollection));
            _addRangeToCollectionMethod = GetType().GetMethod(nameof(AddRangeToCollection));
            _updateInCollectionMethod = GetType().GetMethod(nameof(UpdateInCollection));
            _updateRangeInCollectionMethod = GetType().GetMethod(nameof(UpdateRangeInCollection));
            _removeFromCollectionMethod = GetType().GetMethod(nameof(RemoveFromCollection));
            _removeRangeFromCollectionMethod = GetType().GetMethod(nameof(RemoveRangeFromCollection));
        }

        #region Add
        public Operation<Model> Add<Model>(Model model) 
        where Model : class => Operation.Try(async () =>
        {
            if (model == null)
                throw new ArgumentException("Invalid Model specified: null");

            var transformMap = _entityConfiguration
                .ModelTransformer.Transforms()
                .FirstOrDefault(map => map.ModelType == typeof(Model))
                .ThrowIfNull($"No Transform Map found for model type: {typeof(Model).FullName}");

            //use fast-call to call the AddToCollection<Entity> method on the database object
            var method = _addToCollectionMethod.MakeGenericMethod(transformMap.EntityType);
            var entity = _entityConfiguration.ModelTransformer.ToEntity(model, TransformCommand.Add);
            await this.CallFunc<Task>(method, entity);
            return _entityConfiguration.ModelTransformer.ToModel<Model>(entity, TransformCommand.Add);
        });
        
        public Operation AddBatch<Model>(IEnumerable<Model> models)
        where Model : class => Operation.Try(async () =>
        {
            if (models == null)
                throw new ArgumentException("Invalid model list specified: null");

            var transformMap = _entityConfiguration
                .ModelTransformer.Transforms()
                .FirstOrDefault(map => map.ModelType == typeof(Model))
                .ThrowIfNull($"No Transform Map found for model type: {typeof(Model).FullName}");

            //use fast-call to call the AddToCollection<Entity> method on the database object
            var method = _addRangeToCollectionMethod.MakeGenericMethod(transformMap.EntityType);
            var entities = models
                .Where(model => model != null)
                .Select(model => _entityConfiguration.ModelTransformer.ToEntity(model, TransformCommand.Add));

            await this.CallFunc<Task>(method, entities.ToArray() as object); //so we take the entire array as one parameter into the method
        });

        private async Task AddToCollection<Entity>(Entity entity)
        {
            await _client
                .GetDatabase(_databaseLabel, _settings)
                .GetCollection<Entity>(typeof(Entity).FullName)
                .InsertOneAsync(entity);
        }

        private async Task AddRangeToCollection<Entity>(Entity[] entity)
        {
            await _client
                .GetDatabase(_databaseLabel, _settings)
                .GetCollection<Entity>(typeof(Entity).FullName)
                .InsertManyAsync(entity);
        }
        #endregion

        #region Update
        public Operation<Model> Update<Model>(Model model)
        where Model : class => Operation.Try(async () =>
        {
            if (model == null)
                throw new ArgumentException("Invalid Model specified: null");

            var transformMap = _entityConfiguration
                .ModelTransformer.Transforms()
                .FirstOrDefault(map => map.ModelType == typeof(Model))
                .ThrowIfNull($"No Transform Map found for model type: {typeof(Model).FullName}");

            //use fast-call to call the AddToCollection<Entity> method on the database object
            var method = _updateInCollectionMethod.MakeGenericMethod(transformMap.EntityType);
            var entity = _entityConfiguration.ModelTransformer.ToEntity(model, TransformCommand.Update);
            await this.CallFunc<Task>(method, entity);
            return _entityConfiguration.ModelTransformer.ToModel<Model>(entity, TransformCommand.Update);
        });

        public Operation UpdateBatch<Model>(IEnumerable<Model> models)
        where Model : class => Operation.Try(async () =>
        {
            if (models == null)
                throw new ArgumentException("Invalid Model list specified: null");

            var transformMap = _entityConfiguration
                .ModelTransformer.Transforms()
                .FirstOrDefault(map => map.ModelType == typeof(Model))
                .ThrowIfNull($"No Transform Map found for model type: {typeof(Model).FullName}");

            //use fast-call to call the AddToCollection<Entity> method on the database object
            var method = _updateRangeInCollectionMethod.MakeGenericMethod(transformMap.EntityType);
            var entities = models
                .Where(model => model != null)
                .Select(model => _entityConfiguration.ModelTransformer.ToEntity(model, TransformCommand.Update));

            await this.CallFunc<Task>(method, entities.ToArray() as object); //so we take the entire array as one parameter into the method
        });

        private async Task UpdateInCollection<Entity>(Entity entity)
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

        private async Task UpdateRangeInCollection<Entity>(Entity[] entities)
        {
            var info = _entityConfiguration
                .EntityInfoMap
                .InfoFor<Entity>();

            var replaceModels = entities
                .Select(entity =>
                {
                    var keyValues = info
                        .KeyProperties
                        .Select(propName => entity.PropertyValue(propName))
                        .ToArray();

                    return new ReplaceOneModel<Entity>(info.IdentityFilter<Entity>(keyValues), entity);
                });

            var result = await _client
                .GetDatabase(_databaseLabel, _settings)
                .GetCollection<Entity>(typeof(Entity).FullName)
                .BulkWriteAsync(replaceModels, new BulkWriteOptions{ BypassDocumentValidation =  false });

            //for now, not sure what to use the result for, so i'll ignore...
        }
        #endregion

        #region Delete
        public Operation<Model> Delete<Model>(Model model)
        where Model : class => Operation.Try(async () =>
        {
            if (model == null)
                return null;

            var transformMap = _entityConfiguration
                .ModelTransformer.Transforms()
                .FirstOrDefault(map => map.ModelType == typeof(Model))
                .ThrowIfNull($"No Transform Map found for model type: {typeof(Model).FullName}");

            //use fast-call to call the AddToCollection<Entity> method on the database object
            var method = _removeFromCollectionMethod.MakeGenericMethod(transformMap.EntityType);
            var entity = _entityConfiguration.ModelTransformer.ToEntity(model, TransformCommand.Delete);
            await this.CallFunc<Task>(method, entity);
            return _entityConfiguration.ModelTransformer.ToModel<Model>(entity, TransformCommand.Delete);
        });

        public Operation DeleteBatch<Model>(IEnumerable<Model> models)
        where Model : class => Operation.Try(async () =>
        {
            if (models == null)
                throw new ArgumentException("Invalid Model list specified: null");

            var transformMap = _entityConfiguration
                .ModelTransformer.Transforms()
                .FirstOrDefault(map => map.ModelType == typeof(Model))
                .ThrowIfNull($"No Transform Map found for model type: {typeof(Model).FullName}");

            //use fast-call to call the AddToCollection<Entity> method on the database object
            var method = _removeRangeFromCollectionMethod.MakeGenericMethod(transformMap.EntityType);
            var entities = models
                .Where(model => model != null)
                .Select(model => _entityConfiguration.ModelTransformer.ToEntity(model, TransformCommand.Delete));

            await this.CallFunc<Task>(method, entities.ToArray() as object); //so we take the entire array as one parameter into the method
        });
        

        private async Task RemoveFromCollection<Entity>(Entity entity)
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
                .DeleteOneAsync(filter);
        }

        /// <summary>
        /// Perform a bulk replace
        /// </summary>
        /// <typeparam name="Entity"></typeparam>
        /// <param name="entities"></param>
        /// <returns></returns>
        private async Task RemoveRangeFromCollection<Entity>(Entity[] entities)
        {
            var info = _entityConfiguration
                .EntityInfoMap
                .InfoFor<Entity>();

            var filter = entities
                .Select(entity =>
                {
                    var keyValues = info
                        .KeyProperties
                        .Select(propName => entity.PropertyValue(propName))
                        .ToArray();

                    return info.IdentityFilter<Entity>(keyValues);
                })
                .Pipe(Builders<Entity>.Filter.Or);

            var result = await _client
                .GetDatabase(_databaseLabel, _settings)
                .GetCollection<Entity>(typeof(Entity).FullName)
                .DeleteManyAsync(filter);

            //for now, not sure what to use the result for, so i'll ignore...
        }
        #endregion
    }
}
