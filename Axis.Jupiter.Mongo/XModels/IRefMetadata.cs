using System;

namespace Axis.Jupiter.MongoDb.XModels
{
    public interface IRefMetadata
    {
        Type KeyType { get; }
        Type EntityType { get; }
    }
}
