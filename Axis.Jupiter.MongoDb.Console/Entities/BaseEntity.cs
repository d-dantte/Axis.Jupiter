using Axis.Jupiter.MongoDb.Attributes;
using Axis.Jupiter.MongoDb.XModels;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Axis.Jupiter.MongoDb.ConsoleTest.Entities
{
    public abstract class BaseEntity<TKey>: IMongoEntity<TKey>
    {
        public ObjectId _id { get; set; }

        [MongoIndex]
        public TKey Key { get; set; }

        [JsonIgnore, BsonIgnore]
        TKey IMongoEntity<TKey>.Key { get => Key; set => Key = value; }

        [JsonIgnore, BsonIgnore]
        object IMongoEntity.Key { get => Key; set => Key = (TKey)value; }


        [JsonIgnore, BsonIgnore]
        public bool IsPersisted { get; set; }

        public DateTimeOffset CreatedOn { get; set; }

        public Guid CreatedBy { get; set; }

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
