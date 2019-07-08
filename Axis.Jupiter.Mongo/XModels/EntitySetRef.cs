using Axis.Luna.Extensions;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Axis.Jupiter.MongoDb.XModels
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    public class EntitySetRef<TKey> : IEntitySetRef<TKey, SetRefIdentity<TKey>>
    {
        private readonly HashSet<SetRefIdentity<TKey>> _keys = new HashSet<SetRefIdentity<TKey>>();

        /// <inheritdoc/>
        public string DbCollection { get; }

        /// <inheritdoc/>
        public string DbLabel { get; }

        /// <inheritdoc/>
        public ICollection<SetRefIdentity<TKey>> Refs => _keys; //this property should be serialized as a simple array

        public EntitySetRef(
            string collection,
            string dblabel = null,
            params SetRefIdentity<TKey>[] refIds)
        {
            DbCollection = collection ?? throw new ArgumentNullException(nameof(collection));
            DbLabel = dblabel;
            refIds
                .ThrowIfNull(new ArgumentNullException(nameof(refIds)))
                .ThrowIf(HasNull, new ArgumentException("Invalid RefIdentity found"))
                .Pipe(_keys.AddRange);
        }

        private static bool HasNull<T>(IEnumerable<T> enm) => enm.Any(e => e == null);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    public class SetRefIdentity<TKey> : ISetRefIdentity<TKey>, IRefPointer, IRefPointer<TKey>, IRefMetadata
    {
        /// <inheritdoc/>
        public TKey Key { get; }

        /// <inheritdoc/>
        [BsonIgnore]
        object ISetRefIdentity.Key => Key;

        /// <inheritdoc/>
        [BsonIgnore]
        public Type KeyType => Key.GetType();

        /// <inheritdoc/>
        [BsonIgnore]
        public IMongoEntity<TKey> Entity { get; }

        /// <inheritdoc/>
        [BsonIgnore]
        IMongoEntity IRefPointer.Entity => Entity;

        /// <inheritdoc/>
        [BsonIgnore]
        public Type EntityType => Entity?.GetType();


        public SetRefIdentity(TKey key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            Key = key;
            Entity = null;
        }

        public SetRefIdentity(IMongoEntity<TKey> entity)
        : this(entity.Key)
        {
            Entity = entity;
        }
    }
}
