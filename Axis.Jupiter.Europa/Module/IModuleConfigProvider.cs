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
        /// <typeparam name="Entity"></typeparam>
        /// <param name="configuration"></param>
        /// <returns></returns>
        IModuleConfigProvider UsingConfiguration<Entity>(EntityTypeConfiguration<Entity> configuration) where Entity : class;

        /// <summary>
        /// Adds a new ComplexTypeConfiguratin to the underlying provider's list of configurations for the specified entity type, or replaces the old one.
        /// </summary>
        /// <typeparam name="Entity"></typeparam>
        /// <param name="configuration"></param>
        /// <returns></returns>
        IModuleConfigProvider UsingConfiguration<Entity>(ComplexTypeConfiguration<Entity> configuration) where Entity : class;

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

        void ConfigureContext(DbModelBuilder modelBuilder);
        void InitializeContext(DataStore context);

        /// <summary>
        /// Stops this module config provider from further accepting modifications. Can be called multiple times, but only the 
        /// first call has any effect
        /// </summary>
        void Lock();

        bool IsLocked { get; }
    }
}
