using System;

namespace Axis.Jupiter.MongoDb.Models
{
    public interface IEntityRef
    {
        /// <summary>
        /// The type of the entity that this ref represents
        /// </summary>
        Type EntityType { get; }

        /// <summary>
        /// The type of the key for the entity this ref represents
        /// </summary>
        Type KeyType { get; }

        /// <summary>
        /// The entity itself. Can be null, in which case it represents a "dehydrated" reference - one
        /// which has not been populated by the object it represents
        /// </summary>
        IMongoEntity Entity { get; }

        /// <summary>
        /// Value of the key
        /// </summary>
        object Key { get; }
    }
}
