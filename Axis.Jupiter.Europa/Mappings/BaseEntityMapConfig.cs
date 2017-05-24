using System;
using System.Data.Entity.ModelConfiguration;

namespace Axis.Jupiter.Europa.Mappings
{
    public abstract class BaseEntityMapConfig<Model, Entity> : EntityTypeConfiguration<Entity>, IEntityMapConfiguration
    where Model : class, new()
    where Entity : class, new()
    {
        protected BaseEntityMapConfig(bool useDefaultTable)
        {
            if (useDefaultTable) this.MapToDefaultTable();
        }
        protected BaseEntityMapConfig()
        : this(true)
        { }


        public Type EntityType { get; } = typeof(Entity);
        public Type ModelType { get; } = typeof(Model);


        //void IEntityMapConfiguration.EntityToModelMapper(ModelConverter converter, object entity, object model) => EntityToModel(converter, (Entity)entity, (Model)model);
        //void IEntityMapConfiguration.ModelToEntityMapper(ModelConverter converter, object model, object entity) => ModelToEntity(converter, (Model)model, (Entity)entity);

        //public abstract void EntityToModel(ModelConverter converter, Entity entity, Model model);
        //public abstract void ModelToEntity(ModelConverter converter, Model model, Entity entity);
    }
}
