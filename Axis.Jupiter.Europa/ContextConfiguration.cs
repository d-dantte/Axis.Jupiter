using static Axis.Luna.Extensions.ObjectExtensions;
using static Axis.Luna.Extensions.ExceptionExtensions;

using System;
using System.Collections.Generic;
using System.Data.Entity;
using Axis.Jupiter.Europa.Module;

namespace Axis.Jupiter.Europa
{
    public class ContextConfiguration
    {
        internal dynamic DatabaseInitializer { get; private set; }
        internal string ConnectionString { get; private set; }
        internal Dictionary<string, IModuleConfigProvider> Modules { get; private set; } = new Dictionary<string, IModuleConfigProvider>();


        public ContextConfiguration WithInitializer<Context>(IDatabaseInitializer<Context> initializer)
        where Context : DbContext => this.UsingValue(t => DatabaseInitializer = initializer);

        public ContextConfiguration WithConnection(string entityConnection) => this.UsingValue(t => ConnectionString = entityConnection);

        /// <summary>
        /// Adds a module-configuration to this Context configuration.
        /// </summary>
        /// <param name="seeder"></param>
        /// <returns></returns>
        public ContextConfiguration UsingModule(IModuleConfigProvider module) => this.UsingValue(t => Modules.Add(module.ModuleName, module));
    }
}
