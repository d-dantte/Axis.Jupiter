using System;

namespace Axis.Jupiter.Mongo
{
    public class EntityConfiguration
    {
        public EntityInfoMap EntityInfoMap { get; }
        public ModelTransformer ModelTransformer { get; }

        public EntityConfiguration(ModelTransformer transformer, EntityInfoMap infoMap)
        {
            EntityInfoMap = infoMap ?? throw new Exception("Invalid Entity Info Map specified: null");
            ModelTransformer = transformer ?? throw new Exception("Invalid Model Transformer specified: null");
        }
    }
}
