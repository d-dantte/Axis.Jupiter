using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Axis.Jupiter.MongoDb.Models
{
    public class EntityCollectionRef<TKey, TReferee> : IEntityCollectionRef, IMongoDbEntityRef
    where TReferee : class, IMongoEntity
    {
        private TKey _key;

        /// <summary>
        /// The Entity in which this ref is defined; the owner of the ref
        /// </summary>
        [BsonIgnore]
        public IMongoEntity<TKey> Referrer { get; set; }

        /// <summary>
        /// The value of the Key.
        /// </summary>
        public TKey Key => Referrer == null ? _key : Referrer.Key;

        /// <summary>
        /// The entity that this reference points to.
        /// </summary>
        [BsonIgnore]
        public HashSet<TReferee> EntityCollection { get; } = new HashSet<TReferee>();

        /// <inheritdoc/>
        [BsonIgnore]
        public Type EntityType => typeof(TReferee);

        /// <summary>
        /// 
        /// </summary>
        [BsonIgnore]
        public Type KeyType => typeof(TKey);

        /// <summary>
        /// The entity that this reference points to.
        /// </summary>
        [BsonIgnore]
        IEnumerable<IMongoEntity> IEntityCollectionRef.EntityCollection => EntityCollection.ToArray();

        /// <summary>
        /// The key
        /// </summary>
        [BsonIgnore]
        object IEntityCollectionRef.Key => Key;

        string IMongoDbEntityRef.DbCollection { get; set; }

        string IMongoDbEntityRef.DbLabel { get; set; }

        public EntityCollectionRef(IMongoEntity<TKey> referrer)
        {
            Referrer = referrer ?? throw new ArgumentNullException(nameof(referrer));
        }

        public EntityCollectionRef(TKey key)
        {
            _key = key;
        }
    }
}
