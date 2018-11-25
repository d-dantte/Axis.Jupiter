namespace Axis.Jupiter
{
    public interface IModelTransformer
    {
        object ToEntity<Model>(Model model);
        Entity ToEntity<Model, Entity>(Model model);

        Model ToModel<Entity, Model>(Entity entity);
        Model ToModel<Model>(object entity);
    }
}
