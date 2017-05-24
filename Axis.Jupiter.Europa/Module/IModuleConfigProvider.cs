using Axis.Jupiter.Europa.Mappings;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration;

namespace Axis.Jupiter.Europa.Module
{
    public interface IModuleConfigProvider
    {
        string ModuleName { get; }

        /// <summary>
        /// Adds a new EntityTypeConfiguratin to the underlying provider's list of configurations for the specified entity type, or replaces the old one.
        /// </summary>
        /// <typeparam name="Model"></typeparam>
        /// <typeparam name="Entity"></typeparam>
        /// <param name="configuration"></param>
        /// <returns></returns>
        IModuleConfigProvider UsingConfiguration<Model, Entity>(BaseEntityMapConfig<Model, Entity> configuration)
        where Model : class, new()
        where Entity : class, new();

        /// <summary>
        /// Adds a new ComplexTypeConfiguratin to the underlying provider's list of configurations for the specified entity type, or replaces the old one.
        /// </summary>
        /// <typeparam name="Model"></typeparam>
        /// <typeparam name="Entity"></typeparam>
        /// <param name="configuration"></param>
        /// <returns></returns>
        IModuleConfigProvider UsingConfiguration<Model, Entity>(BaseComplexMapConfig<Model, Entity> configuration)
        where Model : class, new()
        where Entity : class, new();

        /// <summary>
        /// Accepts an action that attempts to seed the database with data - or persorm any other action on the database
        /// </summary>
        /// <typeparam name="Entity"></typeparam>
        /// <param name="seeder"></param>
        /// <returns></returns>
        IModuleConfigProvider WithContextAction(Action<DataStore> seeder);

        /// <summary>
        /// Configuration that doesnt specifically belong to any entity here
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        IModuleConfigProvider UsingModelBuilder(Action<DbModelBuilder> action);

        IEnumerable<Type> ConfiguredTypes();

        void BuildModel(DbModelBuilder modelBuilder);
        void InitializeContext(DataStore context);

        /// <summary>
        /// Determines if this config provider can accept changes to itself or not
        /// </summary>
        bool IsLocked { get; }
    }

    internal interface IEntityMapConfigProvider
    {
        IEnumerable<IEntityMapConfiguration> ConfiguredEntityMaps();
    }
}
