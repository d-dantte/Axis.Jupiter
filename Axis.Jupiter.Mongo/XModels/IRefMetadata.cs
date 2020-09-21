using System;

namespace Axis.Jupiter.MongoDb.XModels
{
    /// <summary>
    /// 
    /// </summary>
    public interface IRefMetadata
    {
        /// <summary>
        /// 
        /// </summary>
        Type KeyType { get; }

        /// <summary>
        /// 
        /// </summary>
        Type EntityType { get; }
    }
}
