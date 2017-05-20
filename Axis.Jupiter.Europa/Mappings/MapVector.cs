using System;

namespace Axis.Jupiter.Europa.Mappings
{

    public class MapVector<Model, Entity>
    where Model : class
    where Entity : class, Model, new()
    {
        public Action<ModelConverter, Model, Entity> ModelToEntity { get; set; }
        public Action<ModelConverter, Entity> EntityToModel { get; set; }

        public bool IsValid() => ModelToEntity != null && EntityToModel != null;
    }
}
