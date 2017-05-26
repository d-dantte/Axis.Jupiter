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

        public void ExtractStoreMetadata(object entity, string serializedMetadata)
        {
            throw new NotImplementedException();
        }

        public string InjectStoreMetadata(object entity)
        {
            throw new NotImplementedException();
        }
    }
}
