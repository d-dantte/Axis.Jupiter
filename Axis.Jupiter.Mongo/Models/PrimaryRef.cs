using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Axis.Jupiter.MongoDb.Models
{
    /// <summary>
    /// Represents a reference to another MongoDb document.
    /// </summary>
    /// <typeparam name="TKey">The Type of the Key used in linking both documents</typeparam>
    public class PrimaryRef<TKey, TReferee>: IEntityRef, IMongoDbEntityRef
    where TReferee: class, IMongoEntity
    {
        private TReferee _referee;
        private TKey _key;
        private readonly Func<TReferee, bool> _notifyAssignment;

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
        public TReferee Entity
        {
            get => _referee;
            set
            {
                if (_notifyAssignment?.Invoke(value) != false)
                    _referee = value;
            }
        }

        /// <inheritdoc/>
        [BsonIgnore]
        public Type EntityType => typeof(TReferee);

        /// <summary>
        /// The type of the key
        /// </summary>
        [BsonIgnore]
        public Type KeyType => typeof(TKey);

        /// <summary>
        /// The entity that this reference points to.
        /// </summary>
        [BsonIgnore]
        IMongoEntity IEntityRef.Entity => Entity;

        /// <summary>
        /// The key
        /// </summary>
        [BsonIgnore]
        object IEntityRef.Key => Key;

        string IMongoDbEntityRef.DbCollection { get; set; }

        string IMongoDbEntityRef.DbLabel { get; set; }

        public PrimaryRef(
            IMongoEntity<TKey> referrer,
            Func<TReferee, bool> notifyAssignment)
        {
            Referrer = referrer ?? throw new ArgumentNullException(nameof(referrer));
            _notifyAssignment = notifyAssignment;
        }

        public PrimaryRef(TKey key)
        {
            _key = key;
        }
    }
}
