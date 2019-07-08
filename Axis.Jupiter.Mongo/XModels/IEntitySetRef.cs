using System.Collections.Generic;

namespace Axis.Jupiter.MongoDb.XModels
{
    public interface IEntitySetRef<out TKey, TRef>
    where TRef : ISetRefIdentity<TKey>
    {
        /// <summary>
        /// The collection to which the referred entity belongs. Should not be null.
        /// </summary>
        string DbCollection { get; }

        /// <summary>
        /// The database within which the entity is located. May be null. Current database is assumed if it is null.
        /// </summary>
        string DbLabel { get; }

        ICollection<TRef> Refs { get; }
    }
    

    public interface ISetRefIdentity
    {
        object Key { get; }
    }

    public interface ISetRefIdentity<out TKey>: ISetRefIdentity
    {
        new TKey Key { get; }
    }
}
