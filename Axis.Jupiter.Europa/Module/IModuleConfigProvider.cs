﻿using System;
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

        void ConfigureContext(DbModelBuilder modelBuilder);
        void SeedContext(EuropaContext context);
    }
}
