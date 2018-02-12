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

        public Func<object, object> EntityFactory { get; protected set; }
        public Func<object, object> ModelFactory { get; protected set; }


        void IEntityMapConfiguration.CopyToModel(object entity, object model, ModelConverter converter) => CopyToModel((Entity)entity, (Model)model, converter);

        void IEntityMapConfiguration.CopyToEntity(object model, object entity, ModelConverter converter) => CopyToEntity((Model)model, (Entity)entity, converter);

        public abstract void CopyToModel(Entity entity, Model model, ModelConverter converter);

        public abstract void CopyToEntity(Model model, Entity entity, ModelConverter converter);
    }
}
