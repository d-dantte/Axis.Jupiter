using System.Collections.Generic;

namespace Axis.Jupiter.MongoDb.XModels
{
    /// <summary>
    /// 
    /// </summary>
    public interface IRefEnumerable
    {
        /// <summary>
        /// 
        /// </summary>
        IEnumerable<ISetRef> Refs { get; }
    }


    /// <summary>
    /// 
    /// </summary>
    public interface IRefEnumerable<out TSRef>: IRefEnumerable
    where TSRef : ISetRef
    {
        /// <summary>
        /// 
        /// </summary>
        new IEnumerable<TSRef> Refs { get; }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TSRef"></typeparam>
    /// <typeparam name="TRefKey"></typeparam>
    public interface IRefEnumerable<out TSRef, out TRefKey>: IRefEnumerable<TSRef>
    where TSRef : ISetRef<TRefKey>
    {
        /// <summary>
        /// 
        /// </summary>
        new IEnumerable<IRefIdentity<TRefKey>> Refs { get; }
    }
}
