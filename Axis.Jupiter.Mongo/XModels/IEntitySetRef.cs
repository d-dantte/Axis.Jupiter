using System.Collections.Generic;

namespace Axis.Jupiter.MongoDb.XModels
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TSRef"></typeparam>
    /// <typeparam name="TRefKey"></typeparam>
    public interface IEntitySetRef<TSRef, TRefInstance, TRefKey, TSourceKey> :
        IRefCollection<TSRef, TRefKey>,
        IRefEnumerable<TSRef, TRefKey>,
        IRefDbInfo
        where TSRef : ISetRef<TRefInstance, TRefKey, TSourceKey>
        where TRefInstance : IMongoEntity<TRefKey>
    {
        /// <summary>
        /// </summary>
        new List<TSRef> Refs { get; }
    }
}
