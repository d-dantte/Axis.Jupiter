using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;

namespace Axis.Jupiter.MongoDb.Models
{
    public interface IMongoEntity
    {
        ObjectId _id { get; set; }

        /// <summary>
        /// Returns all the non-null external document references in this entity
        /// </summary>
        /// <returns></returns>
        IEnumerable<IEntityRef> EntityRefs();

        /// <summary>
        /// Returns all the non-null external document collection references in this entity
        /// </summary>
        /// <returns></returns>
        IEnumerable<IEntityCollectionRef> EntityCollectionRefs();

        int GetHashCode();

        bool Equals(object other);
    }

    public interface IMongoEntity<TKey>: IMongoEntity
    {
        [BsonId]
        TKey Key { get; set; }
    }
}
