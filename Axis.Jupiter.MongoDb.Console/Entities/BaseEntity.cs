using Axis.Jupiter.MongoDb.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace Axis.Jupiter.MongoDb.ConsoleTest.Entities
{
    public abstract class BaseEntity<TKey>: IMongoEntity<TKey>
    {
        public TKey Key { get; set; }

        [BsonIgnore]
        TKey IMongoEntity<TKey>.Key { get => Key; set => Key = value; }

        public ObjectId _id { get; set; } = ObjectId.GenerateNewId();

        public DateTimeOffset CreatedOn { get; set; }

        public Guid CreatedBy { get; set; }

        /// <inheritdoc/>
        public abstract IEnumerable<IEntityCollectionRef> EntityCollectionRefs();
        
        /// <inheritdoc/>
        public abstract IEnumerable<IEntityRef> EntityRefs();

        public override bool Equals(object obj)
        {
            var keyComparer = EqualityComparer<TKey>.Default;
            return obj is BaseEntity<TKey> other
                && other.GetType() == this.GetType()
                && keyComparer.Equals(Key, other.Key);
        }

        public override int GetHashCode() => Key?.GetHashCode() ?? 0;
    }
}
