using System;
using System.Threading.Tasks;
using Axis.Jupiter.MongoDb.Providers;
using Axis.Jupiter.MongoDb.XModels;
using MongoDB.Driver;
using Newtonsoft.Json;

using static Axis.Luna.Extensions.Common;

namespace Axis.Jupiter.MongoDb
{
    public interface IEntityInfo
    {
        Type EntityType { get; }

        Type KeyType { get; }

        string Database { get; }

        string CollectionName { get; }

        bool IsCollectionInitialized { get; }

        MongoDatabaseSettings DatabaseSettings { get; }

        #region Mongo Entity Options
        AggregateOptions QueryOptions { get; }

        CreateIndexOptions KeyIndexOptions { get; }

        DeleteOptions DeleteOptions { get; }

        InsertOneOptions InsertSingleOptions { get; }

        InsertManyOptions InsertMultipleOptions { get; }

        UpdateOptions UpdateOptions { get; }

        JsonConverter Serializer { get; }
        #endregion

        int GetHashCode();

        bool Equals(object other);

        Task InitializeCollection(MongoClient client);
    }


    internal interface IEntityProviderElement
    {
        EntityInfoProvider Provider { set; }
    }


    public abstract class EntityInfo: IEntityInfo, IEntityProviderElement
    {
        private string _collectionName;
        private EntityInfoProvider _infoProvider;

        public Type EntityType { get; }

        public Type KeyType { get; }

        public EntityInfoProvider Provider { get => _infoProvider; }

        EntityInfoProvider IEntityProviderElement.Provider { set => _infoProvider = value; }


        public virtual string Database { get; protected set; }

        public virtual string CollectionName
        {
            get => _collectionName ?? EntityType?.FullName;
            protected set => _collectionName = value;
        }

        public virtual MongoDatabaseSettings DatabaseSettings { get; protected set; }

        public virtual MongoCollectionSettings CollectionSettings { get; protected set; }

        public virtual CreateIndexOptions KeyIndexOptions { get; protected set; }

        public virtual AggregateOptions QueryOptions { get; protected set; }

        public virtual DeleteOptions DeleteOptions { get; protected set; }

        public virtual InsertOneOptions InsertSingleOptions { get; protected set; }

        public virtual InsertManyOptions InsertMultipleOptions { get; protected set; }

        public virtual UpdateOptions UpdateOptions { get; protected set; }

        public JsonConverter Serializer { get; protected set; }

        public abstract bool IsCollectionInitialized { get; protected set; }

        public override bool Equals(object obj)
        {
            return obj is EntityInfo info
                && info.KeyType == KeyType
                && info.EntityType == EntityType;
        }

        public override int GetHashCode() => ValueHash(EntityType, KeyType);

        public abstract Task InitializeCollection(MongoClient client);

        public EntityInfo(Type entityType, Type keyType)
        {
            EntityType = entityType;
            KeyType = keyType;
        }
    }


    public abstract class EntityInfo<TEntity, TKey> : EntityInfo
    where TEntity: IMongoEntity<TKey>, new()
    {
        public override bool IsCollectionInitialized { get; protected set; } = false;

        public EntityInfo()
        : base(typeof(TEntity), typeof(TKey))
        {

        }

        public override bool Equals(object obj)
        {
            return obj is EntityInfo<TEntity, TKey> info
                && info.KeyType == KeyType
                && info.EntityType == EntityType;
        }

        public override int GetHashCode() => base.GetHashCode();        

        public sealed override async Task InitializeCollection(MongoClient client)
        {
            if (!IsCollectionInitialized)
            {
                //may have to also create the collection if it needs to be created first...

                await client
                    .GetDatabase(Database)
                    .GetCollection<TEntity>(CollectionName)
                    .Indexes.CreateOneAsync(new CreateIndexModel<TEntity>(
                        Builders<TEntity>.IndexKeys.Ascending(_ => _.Key),
                        KeyIndexOptions ?? new CreateIndexOptions { Unique = true }));

                IsCollectionInitialized = true;
            }
        }
    }
}
