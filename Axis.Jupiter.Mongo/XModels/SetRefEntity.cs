using Axis.Luna.Extensions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading.Tasks;
using static Axis.Luna.Extensions.Common;

namespace Axis.Jupiter.MongoDb.XModels
{
    public class SetRefEntity<TRefInstance, TRefKey, TSourceKey> : ISetRef<TRefInstance, TRefKey, TSourceKey>
    where TRefInstance: IMongoEntity<TRefKey>
    {
        [JsonIgnore, BsonIgnore]
        private string _string = null;

        public ObjectId _id { get; set; }

        public string Key { get => _string; set => NoOp(); }

        public bool IsPersisted { get; set; }

        /// <inheritdoc/>
        [JsonIgnore, BsonIgnore]
        public TRefInstance RefInstance { get; }

        public TRefKey RefKey { get; }

        public TSourceKey SourceKey { get; }


        #region ISetRefEntity
        
        /// <inheritdoc/>
        [JsonIgnore, BsonIgnore]
        public EntityRef<TRefInstance, TRefKey> TargetRef { get; }

        /// <inheritdoc/>
        [JsonIgnore, BsonIgnore]
        IEntityRef<TRefKey> ISetRef<TRefInstance, TRefKey, TSourceKey>.TargetRef => TargetRef;

        /// <inheritdoc/>
        [JsonIgnore, BsonIgnore]
        IEntityRef ISetRef.TargetRef => TargetRef;

        /// <inheritdoc/>
        [JsonIgnore, BsonIgnore]
        object ISetRef.SourceKey => SourceKey;

        public string RefLabel { get; }
        #endregion

        #region IMongoEntity
        /// <inheritdoc/>
        [JsonIgnore, BsonIgnore]
        string IMongoEntity<string>.Key { get => Key; set => Key = value; }

        /// <inheritdoc/>
        [JsonIgnore, BsonIgnore]
        object IMongoEntity.Key { get => Key; set => Key = (string)value; }
        #endregion

        #region IRefIdentity
        /// <inheritdoc/>
        [JsonIgnore, BsonIgnore]
        object IRefIdentity.RefKey => RefKey;
        #endregion

        #region IRefInstance
        /// <inheritdoc/>
        [JsonIgnore, BsonIgnore]
        IMongoEntity<TRefKey> IRefInstance<TRefKey>.RefInstance => RefInstance;

        /// <inheritdoc/>
        [JsonIgnore, BsonIgnore]
        IMongoEntity IRefInstance.RefInstance => RefInstance;
        #endregion


        public SetRefEntity(
            TSourceKey sourceKey,
            TRefKey refKey,
            string refLabel)
        {
            SourceKey = sourceKey;
            RefKey = refKey;
            RefInstance = default(TRefInstance);
            TargetRef = null;
            RefLabel = refLabel.ThrowIf(
                string.IsNullOrWhiteSpace,
                new Exception("Invalid ref label"));

            _string = $"{SourceKey}::{RefKey}";
        }

        public SetRefEntity(
            TSourceKey sourceKey,
            EntityRef<TRefInstance, TRefKey> targetRef,
            string refLabel)
        {
            SourceKey = sourceKey;
            RefInstance = targetRef.RefInstance;
            RefKey = RefInstance.Key;
            TargetRef = targetRef;
            RefLabel = refLabel.ThrowIf(
                string.IsNullOrWhiteSpace,
                new Exception("Invalid ref label"));

            _string = $"{SourceKey}::{RefKey}";
        }

        public override int GetHashCode() => Common.ValueHash(Key, SourceKey, RefLabel, RefKey, RefInstance);

        public override bool Equals(object obj)
        {
            return obj is SetRefEntity<TRefInstance, TRefKey, TSourceKey> @ref
                && @ref.Key.Equals(Key)
                && @ref.SourceKey.Equals(SourceKey)
                && @ref.RefKey.Equals(RefKey)
                && @ref.RefLabel?.Equals(RefLabel) == null;
        }

        private static void NoOp()
        { }
    }

    public class SetRefEntityInfo<TRefInstance, TRefKey, TSourceInstance, TSourceKey> : EntityInfo
    where TRefInstance : IMongoEntity<TRefKey>
    where TSourceInstance : IMongoEntity<TSourceKey>
    {
        internal SetRefEntityInfo(string databaseName, Providers.EntityInfoProvider infoProvider)
        : base(typeof(SetRefEntity<TRefInstance, TRefKey, TSourceKey>), typeof(Guid))
        {
            Database = databaseName;
            (this as IEntityProviderElement).Provider = infoProvider;

            //other options/config can be set here as needed
        }

        public override string CollectionName
        {
            get => SetRefCollection();
            protected set => base.CollectionName = value;
        }

        public sealed override bool IsCollectionInitialized { get; protected set; }

        public sealed override async Task InitializeCollection(MongoClient client)
        {
            if (!IsCollectionInitialized)
            {
                //may have to also create the collection if it needs to be created first...

                await client
                    .GetDatabase(Database)
                    .GetCollection<SetRefEntity<TRefInstance, TRefKey, TSourceKey>>(CollectionName)
                    .Indexes.CreateOneAsync(new CreateIndexModel<SetRefEntity<TRefInstance, TRefKey, TSourceKey>>(
                        Builders<SetRefEntity<TRefInstance, TRefKey, TSourceKey>>.IndexKeys.Ascending(_ => _.Key),
                        KeyIndexOptions ?? new CreateIndexOptions { Unique = true }));

                IsCollectionInitialized = true;
            }
        }

        private string SetRefCollection()
        {
            var type = typeof(TSourceInstance);
            return $"{type.FullName}.SetRefs";
        }
    }
}
