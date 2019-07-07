using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Axis.Jupiter.MongoDb.Models
{
    /// <summary>
    /// Defines the ref held by the "child" in a "parent-child" reference relationship.
    /// The "key" in this ref is owned by the parent entity. This same key is defined
    /// using a secondary property on the "child" object.
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TRKey"></typeparam>
    /// <typeparam name="TReferee"></typeparam>
    public class SecondaryRef<TKey, TRKey, TReferee> : IEntityRef, IMongoDbEntityRef
    where TReferee : class, IMongoEntity<TKey>
    {
        private TReferee _referee;
        private readonly Func<TReferee, bool> _notifyAssignment;

        /// <summary>
        /// The Entity in which this ref is defined; the owner of the ref
        /// </summary>
        [BsonIgnore]
        public IMongoEntity<TRKey> Referrer { get; set; }

        /// <summary>
        /// The value of the Key.
        /// </summary>
        public TKey Key { get; set; }

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

        public SecondaryRef(
            TKey key,
            string dbLabel,
            string dbCollection)
        {
            Key = key;
            IMongoDbEntityRef @this = this;
            @this.DbCollection = dbCollection ?? throw new ArgumentException(nameof(dbCollection));
            @this.DbLabel = dbLabel ?? throw new ArgumentException(nameof(dbLabel));
        }

        public SecondaryRef(
            IMongoEntity<TRKey> referrer,
            TReferee referee = null,
            string dbLabel = null,
            string dbCollection = null,
            Func<TReferee, bool> notifyAssignment = null)
        {
            Referrer = referrer ?? throw new ArgumentException(nameof(referrer));
            _referee = referee;
            Key = referee.Key;
            _notifyAssignment = notifyAssignment;

            IMongoDbEntityRef @this = this;
            @this.DbCollection = dbCollection;
            @this.DbLabel = dbLabel;
        }
    }
}
