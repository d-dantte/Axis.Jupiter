using System;
using Axis.Jupiter.MongoDb.Models;
using Axis.Jupiter.MongoDb.Serializers;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Axis.Jupiter.MongoDb
{
    public interface IEntityInfo
    {
        Type EntityType { get; }

        Type KeyType { get; }

        string Database { get; }

        string CollectionName { get; }

        MongoDatabaseSettings DatabaseSettings { get; }

        #region Mongo Entity Options
        AggregateOptions QueryOptions { get; }

        DeleteOptions DeleteOptions { get; }

        InsertOneOptions InsertSingleOptions { get; }

        InsertManyOptions InsertMultipleOptions { get; }

        UpdateOptions UpdateOptions { get; }

        IBsonSerializer Serializer { get; }
        #endregion
    }

    public abstract class EntityInfo<TEntity, TKey>: IEntityInfo
    where TEntity: IMongoEntity<TKey>, new()
    {
        private string _collectionName;

        public Type EntityType => typeof(TEntity);

        public Type KeyType => typeof(TKey);

        public virtual string Database { get; protected set; }

        public virtual string CollectionName
        {
            get => _collectionName ?? EntityType?.FullName;
            protected set => _collectionName = value;
        }

        public virtual MongoDatabaseSettings DatabaseSettings { get; protected set; }

        public virtual AggregateOptions QueryOptions { get; protected set; }

        public virtual DeleteOptions DeleteOptions { get; protected set; }

        public virtual InsertOneOptions InsertSingleOptions { get; protected set; }

        public virtual InsertManyOptions InsertMultipleOptions { get; protected set; }

        public virtual UpdateOptions UpdateOptions { get; protected set; }

        IBsonSerializer IEntityInfo.Serializer => Serializer;

        public virtual EntitySerializer<TEntity, TKey> Serializer { get; protected set; } = new EntitySerializer<TEntity, TKey>();
    }
}
