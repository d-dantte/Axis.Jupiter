using System;
using System.Collections.Generic;

namespace Axis.Jupiter.MongoDb.Models
{
    public interface IEntityCollectionRef
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
        /// The collection of referenced entities
        /// </summary>
        IEnumerable<IMongoEntity> EntityCollection { get; }

        object Key { get; }
    }
}
