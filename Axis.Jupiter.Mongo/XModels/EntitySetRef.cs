using Axis.Luna.Extensions;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Axis.Jupiter.MongoDb.XModels
{
    /// <summary>
    /// Represents a reference from an entity to a collection of refs to other entities of a particular type: essentially,
    /// This helps to implement one to many, and many to many relationships.
    /// When used in an entity, this should never be null
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    public class EntitySetRef<TRefInstance, TRefKey, TSourceKey> : IEntitySetRef<SetRefEntity<TRefInstance, TRefKey, TSourceKey>, TRefInstance, TRefKey, TSourceKey>
    where TRefInstance: IMongoEntity<TRefKey>
    {

        /// <inheritdoc/>
        [JsonIgnore, BsonIgnore]
        public List<SetRefEntity<TRefInstance, TRefKey, TSourceKey>> Refs { get; } = new List<SetRefEntity<TRefInstance, TRefKey, TSourceKey>>();

        #region IRefDbInfo
        public string DbCollection { get; set; }

        public string DbLabel { get; set; }
        #endregion

        #region IRefCollection
        /// <inheritdoc/>
        [JsonIgnore, BsonIgnore]
        ICollection<SetRefEntity<TRefInstance, TRefKey, TSourceKey>> IRefCollection<SetRefEntity<TRefInstance, TRefKey, TSourceKey>, TRefKey>.Refs => Refs;
        #endregion

        #region IRefEnumerable
        IEnumerable<IRefIdentity<TRefKey>> IRefEnumerable<SetRefEntity<TRefInstance, TRefKey, TSourceKey>, TRefKey>.Refs => Refs;

        IEnumerable<SetRefEntity<TRefInstance, TRefKey, TSourceKey>> IRefEnumerable<SetRefEntity<TRefInstance, TRefKey, TSourceKey>>.Refs => Refs;

        IEnumerable<ISetRef> IRefEnumerable.Refs => Refs;
        #endregion


        public EntitySetRef(
            string collection,
            params SetRefEntity<TRefInstance, TRefKey, TSourceKey>[] refIds)
            : this(collection, null, refIds)
        {
        }

        public EntitySetRef(
            string collection,
            string dblabel,
            params SetRefEntity<TRefInstance, TRefKey, TSourceKey>[] refIds)
        {
            DbLabel = dblabel;
            DbCollection = collection.ThrowIf(
                string.IsNullOrWhiteSpace,
                new ArgumentNullException(nameof(collection)));
            refIds
                .ThrowIfNull(new ArgumentNullException(nameof(refIds)))
                .ThrowIf(HasNull, new ArgumentException("Invalid RefIdentity found"))
                .Pipe(Refs.AddRange);
        }

        private static bool HasNull<T>(IEnumerable<T> enm) => enm.Any(e => e == null);
    }
}
