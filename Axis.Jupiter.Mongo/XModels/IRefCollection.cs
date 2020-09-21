using System.Collections.Generic;

namespace Axis.Jupiter.MongoDb.XModels
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TSRef"></typeparam>
    /// <typeparam name="TRefKey"></typeparam>
    public interface IRefCollection<TSRef, TRefKey>
    where TSRef: ISetRef<TRefKey>
    {
        /// <summary>
        /// 
        /// </summary>
        ICollection<TSRef> Refs { get; }
    }
}
