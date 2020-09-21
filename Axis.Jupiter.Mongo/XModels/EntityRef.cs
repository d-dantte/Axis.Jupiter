using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using System;

namespace Axis.Jupiter.MongoDb.XModels
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TRefInstance"></typeparam>
    /// <typeparam name="TRefKey"></typeparam>
    public class EntityRef<TRefInstance, TRefKey> : IEntityRef<TRefKey>, IRefInstance<TRefInstance, TRefKey>, IRefMetadata
    where TRefInstance: IMongoEntity<TRefKey>
    {
        #region IEntityRef<>
        /// <inheritdoc/>
        public string DbCollection { get; }

        /// <inheritdoc/>
        public string DbLabel { get; }

        /// <inheritdoc/>
        public TRefKey RefKey { get; }

        /// <inheritdoc/>
        [JsonIgnore, BsonIgnore]
        object IRefIdentity.RefKey => RefKey;
        #endregion


        #region IRefInstance
        /// <inheritdoc/>
        [JsonIgnore, BsonIgnore]
        IMongoEntity IRefInstance.RefInstance => RefInstance;

        /// <inheritdoc/>
        [JsonIgnore, BsonIgnore]
        IMongoEntity<TRefKey> IRefInstance<TRefKey>.RefInstance => RefInstance;

        /// <inheritdoc/>
        [JsonIgnore, BsonIgnore]
        public TRefInstance RefInstance { get; }
        #endregion


        #region IRefMetadata
        /// <inheritdoc/>
        [JsonIgnore, BsonIgnore]
        public Type KeyType => typeof(TRefKey);

        /// <inheritdoc/>
        [JsonIgnore, BsonIgnore]
        public Type EntityType => RefInstance?.GetType();
        #endregion


        public EntityRef<TRefInstance, TRefKey> CloneWith(TRefInstance entity)
        => new EntityRef<TRefInstance, TRefKey>(entity, DbCollection, DbLabel);


        public EntityRef(
            TRefKey key,
            string collection = null,
            string dblabel = null)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            else RefKey = key;

            DbCollection = collection;
            DbLabel = dblabel;
        }

        public EntityRef(
            TRefInstance entity,
            string collection = null,
            string dblabel = null)
            : this(entity.Key, collection, dblabel)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            RefInstance = entity;
        }
    }
}
