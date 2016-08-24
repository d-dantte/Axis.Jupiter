using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Axis.Jupiter.Europa.Module
{
    public interface IModuleConfigProvider
    {
        string ModuleName { get; }

        IEnumerable<KeyValuePair<Type, dynamic>> StoreQueryGenerators { get; }
        IEnumerable<KeyValuePair<string, dynamic>> ContextQueryGenerators { get; }

        /// <summary>
        /// Adds a new EntityTypeConfiguratin to the underlying provider's list of configurations for the specified entity type, or replaces the old one.
        /// </summary>
        /// <typeparam name="Entity"></typeparam>
        /// <param name="configuration"></param>
        /// <returns></returns>
        IModuleConfigProvider UsingConfiguration<Entity>(EntityTypeConfiguration<Entity> configuration) where Entity : class;

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="Entity"></typeparam>
        /// <param name="seeder"></param>
        /// <returns></returns>
        IModuleConfigProvider UsingEntitySeed<Entity>(Action<ObjectStore<Entity>> seeder) where Entity: class;

        /// <summary>
        /// Configuration that doesnt specifically belong to any entity here
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        IModuleConfigProvider UsingModelBuilder(Action<DbModelBuilder> action);

        IEnumerable<Type> ConfiguredTypes();

        void ConfigureContext(DbModelBuilder modelBuilder);
        void InitializeContext(EuropaContext context);
    }
}
