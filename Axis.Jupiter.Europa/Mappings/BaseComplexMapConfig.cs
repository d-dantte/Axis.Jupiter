using System;
using System.Data.Entity.ModelConfiguration;

namespace Axis.Jupiter.Europa.Mappings
{
    public abstract class BaseComplexMapConfig<Model, Entity> : ComplexTypeConfiguration<Entity>, IEntityMapConfiguration
    where Model : class, new()
    where Entity : class, new()
    {
        protected BaseComplexMapConfig()
        {
        }


        public Type EntityType { get; } = typeof(Entity);
        public Type ModelType { get; } = typeof(Model);


        //void IEntityMapConfiguration.EntityToModelMapper(ModelConverter converter, object entity, object model) => EntityToModel(converter, (Entity)entity, (Model)model);
        //void IEntityMapConfiguration.ModelToEntityMapper(ModelConverter converter, object model, object entity) => ModelToEntity(converter, (Model)model, (Entity)entity);

        //public abstract void EntityToModel(ModelConverter converter, Entity entity, Model model);
        //public abstract void ModelToEntity(ModelConverter converter, Model model, Entity entity);
    }
}
