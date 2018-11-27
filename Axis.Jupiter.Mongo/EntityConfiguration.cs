using System;
using Axis.Jupiter.Contracts;

namespace Axis.Jupiter.Mongo
{
    public class EntityConfiguration
    {
        public EntityInfoMap EntityInfoMap { get; }
        public IModelTransformer ModelTransformer { get; }

        public EntityConfiguration(IModelTransformer transformer, EntityInfoMap infoMap)
        {
            EntityInfoMap = infoMap ?? throw new Exception("Invalid Entity Info Map specified: null");
            ModelTransformer = transformer ?? throw new Exception("Invalid Model Transformer specified: null");
        }
    }
}
