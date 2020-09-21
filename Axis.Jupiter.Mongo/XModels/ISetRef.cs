using System;

namespace Axis.Jupiter.MongoDb.XModels
{
    /// <summary>
    /// 
    /// </summary>
    public interface ISetRef : IRefIdentity, IRefInstance, IMongoEntity
    {
        string RefLabel { get; }

        object SourceKey { get; }

        IEntityRef TargetRef { get; }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TRefKey"></typeparam>
    public interface ISetRef<TRefKey> : ISetRef, IRefIdentity<TRefKey>, IRefInstance<TRefKey>, IMongoEntity<string>
    {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TRefInstance"></typeparam>
    /// <typeparam name="TRefKey"></typeparam>
    public interface ISetRef<out TRefInstance, TRefKey, out TSourceKey> : ISetRef<TRefKey>
    where TRefInstance : IMongoEntity<TRefKey>
    {
        new TRefInstance RefInstance { get; }

        new TSourceKey SourceKey { get; }

        new IEntityRef<TRefKey> TargetRef { get; }
    }
}
