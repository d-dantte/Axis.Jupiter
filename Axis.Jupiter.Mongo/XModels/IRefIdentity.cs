
namespace Axis.Jupiter.MongoDb.XModels
{
    /// <summary>
    /// 
    /// </summary>
    public interface IRefIdentity
    {
        /// <summary>
        /// The key that identifies the entity that this reference points to
        /// </summary>
        object RefKey { get; }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    public interface IRefIdentity<out TKey>: IRefIdentity
    {
        /// <summary>
        /// The key that identifies the entity that this reference points to
        /// </summary>
        new TKey RefKey { get; }
    }
}
