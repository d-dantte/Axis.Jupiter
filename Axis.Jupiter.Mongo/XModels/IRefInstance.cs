
namespace Axis.Jupiter.MongoDb.XModels
{
    /// <summary>
    /// 
    /// </summary>
    public interface IRefInstance
    {
        /// <summary>
        /// 
        /// </summary>
        IMongoEntity RefInstance { get; }
    }

    public interface IRefInstance<TKey>: IRefInstance
    {
        /// <summary>
        /// 
        /// </summary>
        new IMongoEntity<TKey> RefInstance { get; }
    }

    public interface IRefInstance<TEntity, TKey>: IRefInstance<TKey>
    where TEntity : IMongoEntity<TKey>
    {
        /// <summary>
        /// 
        /// </summary>
        new TEntity RefInstance { get; }
    }
}
