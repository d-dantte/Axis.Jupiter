using System;

namespace Axis.Jupiter.MongoDb.XModels
{
    /// <summary>
    /// 
    /// </summary>
    public abstract class EntityRef : IEntityRef, IRefMetadata, IRefPointer
    {
        /// <inheritdoc/>
        public string DbCollection { get; }

        /// <inheritdoc/>
        public string DbLabel { get; }

        /// <inheritdoc/>
        public Type KeyType => Key.GetType();

        /// <inheritdoc/>
        public object Key { get; }

        /// <inheritdoc/>
        public Type EntityType => Entity?.GetType();

        /// <inheritdoc/>
        public IMongoEntity Entity { get; }


        protected EntityRef(
            object key,
            string collection,
            string dblabel = null)
        {
            Key = key ?? throw new ArgumentNullException(nameof(key));
            DbCollection = collection ?? throw new ArgumentNullException(nameof(collection));
            DbLabel = dblabel;
        }

        protected EntityRef(
            IMongoEntity entity,
            string collection,
            string dblabel = null)
            : this(entity.Key, collection, dblabel)
        {
            Entity = entity ?? throw new ArgumentNullException(nameof(entity));
        }
    }


    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    public class EntityRef<TKey> : EntityRef, IEntityRef<TKey>, IRefPointer<TKey>
    {
        /// <inheritdoc/>
        new public IMongoEntity<TKey> Entity => (IMongoEntity<TKey>)base.Entity;

        /// <inheritdoc/>
        TKey IEntityRef<TKey>.Key => (TKey)base.Key;


        public EntityRef(
            TKey key,
            string collection,
            string dblabel = null)
            : base(key, collection, dblabel)
        {
        }

        public EntityRef(
            IMongoEntity<TKey> entity,
            string collection,
            string dblabel = null)
            : base(entity, collection, dblabel)
        {
        }
    }
}
