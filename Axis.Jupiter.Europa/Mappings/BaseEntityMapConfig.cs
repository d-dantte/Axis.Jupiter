using System;
using System.Data.Entity.ModelConfiguration;

namespace Axis.Jupiter.Europa.Mappings
{
    public abstract class BaseEntityMapConfig<Model, Entity> : EntityTypeConfiguration<Entity>, IEntityMapConfiguration
    where Model : class
    where Entity : class, Model, new()
    {
        protected BaseEntityMapConfig(bool useDefaultTable, MapVector<Model, Entity> mapper = null)
        {
            if (mapper == null && !this.IsReflexive()) throw new Exception("A mapper function must be supplied for non-reflexive MapConfigurations");
            else if (mapper != null && !mapper.IsValid()) throw new Exception("Invalid MapVector");
            else
            {
                ModelToEntity = (ModelConverter _converter, object _model, object _entity) => mapper.ModelToEntity(_converter, (Model)_model, (Entity)_entity);
                EntityToModel = (ModelConverter _converter, object _entity) => mapper.EntityToModel(_converter, (Entity)_entity);
            }

            if (useDefaultTable) this.MapToDefaultTable();
        }
        protected BaseEntityMapConfig(MapVector<Model, Entity> mapper = null)
        : this(true, mapper)
        { }


        public Type EntityType { get; } = typeof(Entity);
        public Type ModelType { get; } = typeof(Model);

        public Action<ModelConverter, object> EntityToModel { get; private set; }
        public Action<ModelConverter, object, object> ModelToEntity { get; private set; }

    }
}
