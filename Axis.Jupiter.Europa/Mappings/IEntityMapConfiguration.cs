using System;
using System.Collections.Generic;

namespace Axis.Jupiter.Europa.Mappings
{
    internal interface IEntityMapConfiguration
    {
        Type EntityType { get; }
        Type ModelType { get; }

        void CopyToModel(object entity, object model, ModelConverter converter);

        void CopyToEntity(object model, object entity, ModelConverter converter);
    }
}
