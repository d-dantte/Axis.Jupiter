using static Axis.Luna.Extensions.ObjectExtensions;

using System;
using System.Collections.Generic;
using System.Data.Entity;
using Axis.Jupiter.Europa.Module;
using System.Data.Entity.Infrastructure;
using System.Linq;
using Axis.Luna.Extensions;

namespace Axis.Jupiter.Europa
{
    public class ContextConfiguration<Context>
    where Context : DbContext
    {
        private DbModel _model { get; set; }
        private Dictionary<string, IModuleConfigProvider> Modules { get; set; } = new Dictionary<string, IModuleConfigProvider>();

        internal IDatabaseInitializer<Context> DatabaseInitializer { get; private set; }
        internal string ConnectionString { get; private set; }
        internal Action<DbContextConfiguration> EFContextConfiguration { get; set; }


        public IEnumerable<Type> ConfiguredEntityTypes => Modules.Values.SelectMany(_m => _m.ConfiguredTypes());
        public IEnumerable<IModuleConfigProvider> ConfiguredModules => Modules.Values.ToArray();


        public ContextConfiguration<Context> WithInitializer(IDatabaseInitializer<Context> initializer) => this.UsingValue(_ => DatabaseInitializer = initializer);

        public ContextConfiguration<Context> WithConnection(string entityConnection) => this.UsingValue(_ => ConnectionString = entityConnection);

        public ContextConfiguration<Context> WithEFConfiguraton(Action<DbContextConfiguration> contextConfig) => this.UsingValue(_ => EFContextConfiguration = contextConfig);

        public ContextConfiguration<Context> UsingModule(IModuleConfigProvider module) => this.UsingValue(_ => Modules.Add(module.ModuleName, module));
    }
}
