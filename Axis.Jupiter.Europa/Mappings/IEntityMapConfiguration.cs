using System;

namespace Axis.Jupiter.Europa.Mappings
{
    internal interface IEntityMapConfiguration
    {
        Type EntityType { get; }
        Type ModelType { get; }

        void ExtractStoreMetadata(object entity, string serializedMetadata);
        string InjectStoreMetadata(object entity);
    }
}
