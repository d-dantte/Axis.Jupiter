using System;

namespace Axis.Jupiter.Europa.Mappings
{

    public interface IEntityMapConfiguration
    {
        Type EntityType { get; }
        Type ModelType { get; }

        Action<ModelConverter, object> EntityToModel { get; }
        Action<ModelConverter, object, object> ModelToEntity { get; }
    }
}
