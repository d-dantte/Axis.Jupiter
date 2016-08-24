using static Axis.Luna.Extensions.ObjectExtensions;
using static Axis.Luna.Extensions.ExceptionExtensions;

using System;
using System.Collections.Generic;
using System.Data.Entity;
using Axis.Jupiter.Europa.Module;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;
using System.Data.Entity.ModelConfiguration;
using Axis.Luna.Extensions;
using Axis.Jupiter.Europa.Utils;

namespace Axis.Jupiter.Europa
{
    public class ContextConfiguration<Context>
    where Context: DbContext
    {
        private DbModel _model { get; set; }
        private DbCompiledModel _compiledModel { get; set; }
        private Dictionary<string, IModuleConfigProvider> Modules { get; set; } = new Dictionary<string, IModuleConfigProvider>();

        internal IDatabaseInitializer<Context> DatabaseInitializer { get; private set; }
        internal string ConnectionString { get; private set; }
        internal SqlBulkCopyOptions BulkCopyOptions { get; private set; }
        internal Action<DbContextConfiguration> EFContextConfiguration { get; set; }

        public EFMappings EFMappings { get; private set; }


        public IEnumerable<Type> ConfiguredEntityTypes => Modules.Values.SelectMany(_m => _m.ConfiguredTypes());
        public IEnumerable<IModuleConfigProvider> ConfiguredModules => Modules.Values.ToArray();


        public ContextConfiguration<Context> WithInitializer(IDatabaseInitializer<Context> initializer)
            => this.UsingValue(t => DatabaseInitializer = initializer);

        public ContextConfiguration<Context> WithConnection(string entityConnection) => this.UsingValue(t => ConnectionString = entityConnection);

        public ContextConfiguration<Context> WithEFConfiguraton(Action<DbContextConfiguration> contextConfig) => this.UsingValue(t => EFContextConfiguration = contextConfig);

        public ContextConfiguration<Context> WithBulkCopyOptions(SqlBulkCopyOptions options) => this.UsingValue(_v => BulkCopyOptions = options);
        
        public ContextConfiguration<Context> UsingModule(IModuleConfigProvider module) => this.UsingValue(t => Modules.Add(module.ModuleName, module));


        public DbCompiledModel Compile(DbModelBuilder builder)
        {
            if (_compiledModel == null)
            {
                ConfiguredModules.ForAll((_cnt, _module) => _module.ConfigureContext(builder));

                new SqlConnection(ConnectionString).Using(sql =>
                {
                    _model = builder.Build(sql);
                });

                _compiledModel = _model.Compile();
                EFMappings = new EFMappings(_model);
            }

            return _compiledModel;
        }
        public DbCompiledModel Compile() => Compile(new DbModelBuilder(DbModelBuilderVersion.Latest));
    }
}
