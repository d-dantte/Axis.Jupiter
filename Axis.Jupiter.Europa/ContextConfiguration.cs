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


        public ContextConfiguration<Context> WithInitializer(IDatabaseInitializer<Context> initializer) => Try(() => DatabaseInitializer = initializer);

        public ContextConfiguration<Context> WithConnection(string entityConnection) => Try(() => ConnectionString = entityConnection);

        public ContextConfiguration<Context> WithEFConfiguraton(Action<DbContextConfiguration> contextConfig) => Try(() => EFContextConfiguration = contextConfig);

        public ContextConfiguration<Context> WithBulkCopyOptions(SqlBulkCopyOptions options) => Try(() => BulkCopyOptions = options);
        
        public ContextConfiguration<Context> UsingModule(IModuleConfigProvider module) => Try(() => Modules.Add(module.ModuleName, module));

        /// <summary>
        /// Compiles this configuration, prohibiting further modification, and returning a compiled model that Entity Framework uses to create the DbContext.
        /// Calling this method multiple times has no effect, as only the first call has any effect.
        /// </summary>
        /// <param name="builder">a model builder to use in generating the DbModel used internally</param>
        /// <returns></returns>
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

                //lock module config providers
                ConfiguredModules.ForAll((_cnt, _module) => _module.Lock());
            }

            return _compiledModel;
        }

        /// <summary>
        /// Compiles this configuration, prohibiting further modification, and returning a compiled model that Entity Framework uses to create the DbContext.
        /// Calling this method multiple times has no effect, as only the first call has any effect.
        /// </summary>
        /// <returns></returns>
        public DbCompiledModel Compile() => Compile(new DbModelBuilder(DbModelBuilderVersion.Latest));

        private ContextConfiguration<Context> Try(Action action)
        {
            if (_compiledModel != null) throw new Exception("Attempting to modify a compiled configuratin");
            //else

            action();
            return this;
        }
    }
}
