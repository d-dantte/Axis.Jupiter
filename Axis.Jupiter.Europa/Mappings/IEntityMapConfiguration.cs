using System;

namespace Axis.Jupiter.Europa.Mappings
{
    internal interface IEntityMapConfiguration
    {
        Type EntityType { get; }
        Type ModelType { get; }

        void EntityToModelMapper(ModelConverter converter, object entity);
        void ModelToEntityMapper(ModelConverter converter, object model, object entity);
    }
}
