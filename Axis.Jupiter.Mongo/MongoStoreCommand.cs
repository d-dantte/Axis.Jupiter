using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Axis.Jupiter.Contracts;
using Axis.Jupiter.Helpers;
using Axis.Jupiter.Models;
using Axis.Jupiter.MongoDb.Models;
using Axis.Jupiter.MongoDb.Providers;
using Axis.Jupiter.Services;
using Axis.Luna.Extensions;
using Axis.Luna.Operation;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace Axis.Jupiter.MongoDb
{
    public class MongoStoreCommand: IStoreCommand
    {
        private static readonly ConcurrentDictionary<string, MethodInfo> _methodCache = new ConcurrentDictionary<string, MethodInfo>();
        private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _propertyCache = new ConcurrentDictionary<Type, PropertyInfo[]>();

        private readonly MongoClient _client;
        private readonly EntityMapper _mapper;
        private readonly EntityInfoProvider _infoProvider;

        #region Fast Call enablers

        private readonly MethodInfo _addToCollectionMethod;
        private readonly MethodInfo _addRangeToCollectionMethod;
        private readonly MethodInfo _updateInCollectionMethod;
        private readonly MethodInfo _updateRangeInCollectionMethod;
        private readonly MethodInfo _removeFromCollectionMethod;
        private readonly MethodInfo _removeRangeFromCollectionMethod;

        #endregion

        public MongoStoreCommand(
            EntityMapper transformer,
            EntityInfoProvider infoProvider,
            MongoClient client)
        {
            _client = client ?? throw new ArgumentException("Invalid Client specified: null");
            _mapper = transformer ?? throw new Exception("Invalid Transformer specified: null");
            _infoProvider = infoProvider ?? throw new Exception("Invalid Entity Info Provider");

            var methods = GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic);

            _addToCollectionMethod = methods.First(IsAddToCollection);
            _addRangeToCollectionMethod = methods.First(IsAddRangeToCollection);
            _updateInCollectionMethod = methods.First(IsUpdateInCollection);
            _updateRangeInCollectionMethod = methods.First(IsUpdateRangeInCollection);
            _removeFromCollectionMethod = methods.First(IsRemoveFromCollection);
            _removeRangeFromCollectionMethod = methods.First(IsRemoveRangeFromCollection);
        }

        #region Add
        public Operation<Model> Add<Model>(Model model) 
        where Model : class 
        => Operation.Try(async () =>
        {
            if (model == null)
                throw new ArgumentException("Invalid Model specified: null");

            var entity = _mapper
                .ToEntity(model, MappingIntent.Add)
                .As<IMongoEntity>();

            var entityGroups = MongoStoreCommand
                .ExtractExternalReferences(entity, _infoProvider)
                .GroupBy(EntityInfo);

            //now add each group to their respective collections
            await entityGroups.ForAllAsync(group =>
            {
                //grab the generic form of the add-method.
                var method = _methodCache.GetOrAdd(
                    GenericSignature(_addRangeToCollectionMethod, group.Key.EntityType),
                    _ => _addRangeToCollectionMethod.MakeGenericMethod(group.Key.EntityType));

                //use fast-call to call the AddToCollection<Entity> method on the database object
                //cast the group so it is seen as a single arument
                return this.CallFunc<Task>(method, group.Cast<object>().ToArray() as object);
            });

            //We assume that the object(s) comes back with any automatically set ids
            return _mapper.ToModel<Model>(entity, MappingIntent.Add);
        });
        
        public Operation AddBatch<Model>(IEnumerable<Model> models)
        where Model : class 
        => Operation.Try(async () =>
        {
            if (models == null)
                throw new ArgumentException("Invalid Model specified: null");

            await models
                .Select(model => _mapper.ToEntity(model, MappingIntent.Add))
                .Pipe(AddBatch);
        });

        private Operation AddBatch(IEnumerable<object> entities)
        => Operation.Try(async () =>
        {
            if (entities == null || entities.Any(IsNull))
                throw new ArgumentException("Invalid entities");

            var entityGroups = entities
                .Cast<IMongoEntity>()
                .SelectMany(e => ExtractExternalReferences(e, _infoProvider))
                .GroupBy(EntityInfo);

            //now add each group to their respective collections
            await entityGroups.ForAllAsync(group =>
            {
                //grab the generic form of the add-method.
                var method = _methodCache.GetOrAdd(
                    GenericSignature(_addRangeToCollectionMethod, group.Key.EntityType),
                    _ => _addRangeToCollectionMethod.MakeGenericMethod(group.Key.EntityType));

                //use fast-call to call the AddToCollection<Entity> method on the database object
                //cast the group so it is seen as a single argument
                return this.CallFunc<Task>(method, group.Cast<object>().ToArray() as object);
            });
        });

        private async Task AddToCollection<Entity>(object entity)
        {
            if (entity == null)
                return;

            var info = _infoProvider
                .InfoForEntity<Entity>()
                .ThrowIfNull("No EntityInfo found for: " + typeof(Entity));

            await _client
                .GetDatabase(info.Database, info.DatabaseSettings)
                .GetCollection<Entity>(info.CollectionName)
                .InsertOneAsync((Entity)entity, info.InsertSingleOptions);
        }

        private async Task AddRangeToCollection<Entity>(object[] entities)
        {
            var info = _infoProvider
                .InfoForEntity<Entity>()
                .ThrowIfNull("No EntityInfo found for: " + typeof(Entity));

            await _client
                .GetDatabase(info.Database, info.DatabaseSettings)
                .GetCollection<Entity>(info.CollectionName)
                .InsertManyAsync(entities.Cast<Entity>(), info.InsertMultipleOptions);
        }
        #endregion

        #region Update
        public Operation<Model> Update<Model>(Model model)
        where Model : class 
        => Operation.Try(async () =>
        {
            if (model == null)
                throw new ArgumentException("Invalid Model specified: null");

            var entity = _mapper
                .ToEntity(model, MappingIntent.Add)
                .As<IMongoEntity>();
                        
            var entityGroups = MongoStoreCommand
                .ExtractExternalReferences(entity, _infoProvider)
                .GroupBy(EntityInfo);

            //now update each group in their respective collections
            await entityGroups.ForAllAsync(group =>
            {
                var info = group.Key;

                //grab the generic form of the add-method.
                var method = _methodCache.GetOrAdd(
                    GenericSignature(_updateRangeInCollectionMethod, info.EntityType),
                    _ => _updateRangeInCollectionMethod.MakeGenericMethod(info.EntityType, info.KeyType));

                //use fast-call to call the AddToCollection<Entity> method on the database object
                //cast the group so it is seen as a single arument
                return this.CallFunc<Task>(method, group.Cast<object>().ToArray() as object);
            });

            //We assume that the object(s) comes back with any automatically set ids
            return _mapper.ToModel<Model>(entity, MappingIntent.Add);
        });

        public Operation UpdateBatch<Model>(IEnumerable<Model> models)
        where Model : class 
        => Operation.Try(async () =>
        {
            if (models == null)
                throw new ArgumentException("Invalid Model specified: null");

            await models
                .Select(model => _mapper.ToEntity(model, MappingIntent.Add))
                .Pipe(UpdateBatch);
        });

        private Operation UpdateBatch(IEnumerable<object> entities)
        => Operation.Try(async () =>
        {
            if (entities == null || entities.Any(IsNull))
                throw new ArgumentException("Invalid Entities");

            var entityGroups = entities
                .Cast<IMongoEntity>()
                .SelectMany(e => ExtractExternalReferences(e, _infoProvider))
                .GroupBy(EntityInfo); //if there's something cheaper than the info to group by, use it.

            //now update each group in their respective collections
            await entityGroups.ForAllAsync(group =>
            {
                var info = group.Key;

                //grab the generic form of the add-method.
                var method = _methodCache.GetOrAdd(
                    GenericSignature(_updateRangeInCollectionMethod, info.EntityType),
                    _ => _updateRangeInCollectionMethod.MakeGenericMethod(info.EntityType, info.KeyType));

                //use fast-call to call the AddToCollection<Entity> method on the database object
                //cast the group so it is seen as a single argument
                return this.CallFunc<Task>(method, group.Cast<object>().ToArray() as object);
            });
        });

        private async Task UpdateInCollection<Entity, TKey>(object entity)
        where Entity: IMongoEntity<TKey>
        {
            var info = _infoProvider
                .InfoForEntity<Entity>()
                .ThrowIfNull("No EntityInfo found for: " + typeof(Entity));

            var mongoEntity = (Entity)entity;
            var filter = Builders<Entity>.Filter.Eq(e => e.Key, mongoEntity.Key);

            //investigate building an update model for all properties rather than doing a complete replacement:
            //reason for this is that a replace may (??) replace the mongo id also, thus destroying any links
            //to the document that use that mongo id.
            await _client
                .GetDatabase(info.Database, info.DatabaseSettings)
                .GetCollection<Entity>(info.CollectionName)
                .ReplaceOneAsync(filter, mongoEntity, info.UpdateOptions); 
        }

        private async Task UpdateRangeInCollection<Entity, TKey>(object[] entities)
        where Entity: IMongoEntity<TKey>
        {
            var info = _infoProvider
                .InfoForEntity<Entity>()
                .ThrowIfNull("No EntityInfo found for: " + typeof(Entity));

            var replaceModels = entities
                .Cast<Entity>()
                .Select(entity =>
                {
                    var filter = Builders<Entity>.Filter.Eq(e => e.Key, entity.Key);
                    var updateModel = GenerateUpdateModelFor(filter, entity);

                    updateModel.Collation = info.UpdateOptions?.Collation ?? updateModel.Collation;
                    updateModel.IsUpsert = info.UpdateOptions?.IsUpsert ?? false;
                    updateModel.ArrayFilters = info.UpdateOptions?.ArrayFilters ?? updateModel.ArrayFilters;

                    return updateModel;
                })
                .ToArray();
            
            var result = await _client
                .GetDatabase(info.Database, info.DatabaseSettings)
                .GetCollection<Entity>(info.CollectionName)
                .BulkWriteAsync(replaceModels, new BulkWriteOptions{ BypassDocumentValidation =  false });

            //for now, not sure what to use the result for, so i'll ignore...
        }
        #endregion

        #region Delete
        public Operation<Model> Delete<Model>(Model model)
        where Model : class 
        => Operation.Try(async () =>
        {
            if (model == null)
                return null;

            var entityEntry = _mapper
                .EntryForModel<Model>()
                .ThrowIfNull($"No Transform Map found for model type: {typeof(Model).FullName}");

            var info = _infoProvider
                .InfoForEntity(entityEntry.TypeMapper.EntityType)
                .ThrowIfNull($"No Entity Info found for: {entityEntry.TypeMapper.EntityType.FullName}");

            //use fast-call to call the AddToCollection<Entity> method on the database object
            var method = _methodCache.GetOrAdd(
                    GenericSignature(_removeFromCollectionMethod, info.EntityType),
                    _ => _removeFromCollectionMethod.MakeGenericMethod(info.EntityType, info.KeyType));

            var entity = _mapper.ToEntity(model, MappingIntent.Remove);
            await this.CallFunc<Task>(method, entity);

            return _mapper.ToModel<Model>(entity, MappingIntent.Remove);
        });

        public Operation DeleteBatch<Model>(IEnumerable<Model> models)
        where Model : class 
        => Operation.Try(async () =>
        {
            if (models == null || models.Any(IsNull))
                throw new ArgumentException("Invalid Model list specified: null");

            await models
                .Select(model => _mapper.ToEntity(model, MappingIntent.Add))
                .Pipe(DeleteBatch);
        });

        private Operation DeleteBatch(IEnumerable<object> entities)
        => Operation.Try(async () =>
        {
            if (entities == null || entities.Any(IsNull))
                throw new ArgumentException("Invalid entities");

            var entityGroups = entities
                .Cast<IMongoEntity>()
                .GroupBy(EntityInfo);

            //now add each group to their respective collections
            await entityGroups.ForAllAsync(group =>
            {
                //grab the generic form of the add-method.
                var method = _methodCache.GetOrAdd(
                    GenericSignature(_removeRangeFromCollectionMethod, group.Key.EntityType),
                    _ => _removeRangeFromCollectionMethod.MakeGenericMethod(group.Key.EntityType));

                //use fast-call to call the AddToCollection<Entity> method on the database object
                //cast the group so it is seen as a single argument
                return this.CallFunc<Task>(method, group.Cast<object>().ToArray() as object);
            });
        });

        private async Task RemoveFromCollection<Entity, TKey>(Entity entity)
        where Entity: IMongoEntity<TKey>
        {
            var info = _infoProvider
                .InfoForEntity<Entity>()
                .ThrowIfNull("No EntityInfo found for: " + typeof(Entity));

            var filter = Builders<Entity>.Filter.Eq(e => e.Key, entity.Key);

            await _client
                .GetDatabase(info.Database, info.DatabaseSettings)
                .GetCollection<Entity>(info.CollectionName)
                .DeleteOneAsync(filter);
        }

        private async Task RemoveRangeFromCollection<Entity, TKey>(Entity[] entities)
        where Entity: IMongoEntity<TKey>
        {
            var info = _infoProvider
                .InfoForEntity<Entity>()
                .ThrowIfNull("No EntityInfo found for: " + typeof(Entity));

            var filter = entities
                .Select(entity => Builders<Entity>.Filter.Eq(e => e.Key, entity.Key))
                .Pipe(Builders<Entity>.Filter.Or);

            var result = await _client
                .GetDatabase(info.Database, info.DatabaseSettings)
                .GetCollection<Entity>(info.CollectionName)
                .DeleteManyAsync(filter);

            //for now, not sure what to use the result for, so i'll ignore...
        }
        #endregion
        
        #region Collection 
        public Operation AddToCollection<Parent, Child>(
            Parent parent,
            Expression<Func<Parent, ICollection<Child>>> collectionPropertyExpression,
            params Child[] children)
            where Parent : class
            where Child : class
        => Operation.Try(async () =>
        {
            var infos = _mapper
                .ToCollectionRefInfo(
                    parent,
                    MappingIntent.Add,
                    collectionPropertyExpression,
                    children)
                .ToArray();

            await infos
                .GroupBy(info => info.Rank)
                .OrderByDescending(group => group.Key)
                .Select(async group =>
                {
                    await group
                        .GroupBy(info => info.Command)
                        .OrderBy(group2 => group2.Key)
                        .Select(async group2 =>
                        {
                            switch (group2.Key)
                            {
                                case CollectionRefCommand.Add:
                                    var arr = group2
                                        .Select(info => info.Entity)
                                        .ToArray();

                                    await AddBatch(arr);
                                    break;

                                case CollectionRefCommand.Update:
                                    arr = group2
                                        .Select(info => info.Entity)
                                        .ToArray();

                                    await UpdateBatch(arr);
                                    break;

                                default:
                                    throw new Exception("Invalid Command: " + group.Key);
                            }
                        })
                        .Fold();
                })
                .Fold();

            var property = collectionPropertyExpression.Body
                .As<MemberExpression>().Member
                .As<PropertyInfo>();
            var modelCollection = parent.PropertyValue(property.Name) as ICollection<Child>;

            //add the children to the collection
            infos
                .Where(info => info.Result != RefInfoResult.None)
                .GroupBy(info => info.Result)
                .ForAll(group =>
                {
                    switch (group.Key)
                    {
                        case RefInfoResult.Entity:
                            modelCollection.AddRange(group.Select(info => _mapper.ToModel<Child>(
                                info.Entity,
                                MappingIntent.Add)));
                            break;

                        case RefInfoResult.Model:
                            modelCollection.AddRange(group.Select(info => info.Model as Child));
                            break;
                    }
                });
        });

        public Operation RemoveFromCollection<Parent, Child>(
            Parent parent,
            Expression<Func<Parent, ICollection<Child>>> collectionPropertyExpression,
            params Child[] children)
            where Parent : class
            where Child : class
        => Operation.Try(async () =>
        {
            var infos = _mapper.ToCollectionRefInfo(
                parent,
                MappingIntent.Add,
                collectionPropertyExpression,
                children);

            await infos
                .GroupBy(info => info.Rank)
                .OrderByDescending(group => group.Key)
                .Select(async group =>
                {
                    await group
                        .GroupBy(info => info.Command)
                        .OrderBy(group2 => group2.Key)
                        .Select(async group2 =>
                        {
                            switch (group2.Key)
                            {
                                case CollectionRefCommand.Remove:
                                    var arr = group
                                        .Select(info => info.Entity)
                                        .ToArray();

                                    await DeleteBatch(arr);
                                    break;

                                case CollectionRefCommand.Update:
                                    arr = group
                                        .Select(info => info.Entity)
                                        .ToArray();

                                    await UpdateBatch(arr);
                                    break;

                                default: throw new Exception("Invalid Command: " + group.Key);
                            }
                        })
                        .Fold();
                })
                .Fold();

            var property = collectionPropertyExpression.Body
                .As<MemberExpression>().Member
                .As<PropertyInfo>();

            var modelCollection = parent.PropertyValue(property.Name) as ICollection<Child>;
            modelCollection.RemoveAll(children);
        });
        #endregion

        #region Helpers

        private IEntityInfo EntityInfo(IMongoEntity entity)
        => entity == null? null : _infoProvider.InfoForEntity(entity.GetType());

        private static bool IsNotNull<T>(T t) => t != null;

        private static bool IsNull<T>(T t) => t == null;

        private static IEnumerable<IMongoEntity> ExtractExternalReferences(IMongoEntity entity, EntityInfoProvider provider)
        => ExtractExternalReferences(entity, null, provider);

        private static IEnumerable<IMongoEntity> ExtractExternalReferences(IMongoEntity entity, HashSet<IMongoEntity> context, EntityInfoProvider provider)
        {
            context = context ?? new HashSet<IMongoEntity>();

            if (entity == null)
                return context;

            else if (context.Contains(entity))
                return context;

            else
            {
                context.Add(entity);

                //refs
                entity
                    .EntityRefs()
                    .Where(@ref => @ref.Entity != null)
                    .Select(@ref => InitializeRef(@ref, provider))
                    .Select(@ref => @ref.Entity)
                    .ForAll(e => ExtractExternalReferences(e, context, provider));

                //collection refs
                entity
                    .EntityCollectionRefs()
                    .Select(@ref => InitializeRef(@ref, provider))
                    .SelectMany(@ref => @ref.EntityCollection
                    .Where(IsNotNull))
                    .ForAll(e => ExtractExternalReferences(e, context, provider));

                return context;
            }
        }
    
        private static IEntityRef InitializeRef(IEntityRef @ref, EntityInfoProvider provider)
        {
            var info = provider.InfoForEntity(@ref.EntityType);
            var mongoRef = @ref as IMongoDbEntityRef;
            mongoRef.DbLabel = info.Database;
            mongoRef.DbCollection = info.CollectionName;

            return @ref;
        }

        private static IEntityCollectionRef InitializeRef(IEntityCollectionRef @ref, EntityInfoProvider provider)
        {
            var info = provider.InfoForEntity(@ref.EntityType);
            var mongoRef = @ref as IMongoDbEntityRef;
            mongoRef.DbLabel = info.Database;
            mongoRef.DbCollection = info.CollectionName;

            return @ref;
        }

        private static string GenericSignature(
            MethodInfo method, 
            Type genericType)
            => $"{method.Name}<{genericType.MinimalAQName()}>";

        private static bool IsAddToCollection(MethodInfo method)
        {
            return method.Name == nameof(AddToCollection)
                && method.IsGenericMethodDefinition
                && method.GetGenericArguments().Length == 1
                && method.ReturnType == typeof(Task);
        }

        private static bool IsAddRangeToCollection(MethodInfo method)
        {
            return method.Name == nameof(AddRangeToCollection)
                && method.IsGenericMethodDefinition
                && method.GetGenericArguments().Length == 1
                && method.ReturnType == typeof(Task);
        }

        private static bool IsUpdateInCollection(MethodInfo method)
        {
            return method.Name == nameof(UpdateInCollection)
                && method.IsGenericMethodDefinition
                && method.GetGenericArguments().Length == 2
                && method.ReturnType == typeof(Task);
        }

        private static bool IsUpdateRangeInCollection(MethodInfo method)
        {
            return method.Name == nameof(UpdateRangeInCollection)
                && method.IsGenericMethodDefinition
                && method.GetGenericArguments().Length == 2
                && method.ReturnType == typeof(Task);
        }

        private static bool IsRemoveFromCollection(MethodInfo method)
        {
            return method.Name == nameof(RemoveFromCollection)
                && method.IsGenericMethodDefinition
                && method.GetGenericArguments().Length == 2
                && method.ReturnType == typeof(Task);
        }

        private static bool IsRemoveRangeFromCollection(MethodInfo method)
        {
            return method.Name == nameof(RemoveRangeFromCollection)
                && method.IsGenericMethodDefinition
                && method.GetGenericArguments().Length == 2
                && method.ReturnType == typeof(Task);
        }

        private static bool IsNotIgnored(PropertyInfo property)
        => !property.IsDefined(typeof(BsonIgnoreAttribute), true);

        private static bool IsNotId(PropertyInfo property)
        => !property.Name.Equals("_id");

        private static UpdateOneModel<Entity> GenerateUpdateModelFor<Entity>(FilterDefinition<Entity> filterDef, Entity entity)
        where Entity: IMongoEntity
        {
            var properties = _propertyCache.GetOrAdd(
                typeof(Entity),
                _type => _type
                    .GetProperties()
                    .Where(IsNotIgnored)
                    .ToArray());

            var builder = Builders<Entity>.Update;
            var definition = properties
                .Where(IsNotId)
                .Aggregate(null as UpdateDefinition<Entity>, (def, prop) =>
                {
                    if (def == null)
                        return builder.Set(prop.Name, entity.PropertyValue(prop.Name));

                    else
                        return def.Set(prop.Name, entity.PropertyValue(prop.Name));
                });

            return new UpdateOneModel<Entity>(filterDef, definition);
        }
        #endregion
    }
}
