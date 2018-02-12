using System;

namespace Axis.Jupiter.Europa.Mappings
{
    internal interface IEntityMapConfiguration
    {
        Type EntityType { get; }
        Type ModelType { get; }

        Func<object, object> EntityFactory { get; }
        Func<object, object> ModelFactory { get; }

        void CopyToModel(object entity, object model, ModelConverter converter);
        void CopyToEntity(object model, object entity, ModelConverter converter);
    }
}
