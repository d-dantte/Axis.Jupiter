
namespace Axis.Jupiter.MongoDb.XModels
{
    public interface IEntityRef
    {
        /// <summary>
        /// The collection to which the referred entity belongs. Should not be null.
        /// </summary>
        string DbCollection { get; }

        /// <summary>
        /// The database within which the entity is located. May be null. Current database is assumed if it is null.
        /// </summary>
        string DbLabel { get; }

        /// <summary>
        /// The key that identifies the entity that this reference points to
        /// </summary>
        object Key { get; }
    }


    public interface IEntityRef<TKey>: IEntityRef
    {
        /// <inheritdoc/>
        new TKey Key { get; }
    }
}
