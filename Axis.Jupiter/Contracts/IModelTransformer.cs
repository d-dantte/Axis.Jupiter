using System;

namespace Axis.Jupiter.Contracts
{
    public interface IModelTransformer
    {
        object ToEntity<Model>(Model model);
        Entity ToEntity<Model, Entity>(Model model);

        Model ToModel<Entity, Model>(Entity entity);
        Model ToModel<Model>(object entity);

        TransformMap[] Transforms();
    }

    public class TransformMap
    {
        public Type ModelType { get; set; }
        public Type EntityType { get; set; }
    }
}
