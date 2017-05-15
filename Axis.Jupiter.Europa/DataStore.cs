using Axis.Luna.Extensions;
using System;
using System.Data.Entity;

namespace Axis.Jupiter.Europa
{
    public class DataStore : DbContext, IDisposable
    {
        #region Properties
        internal ContextConfiguration<DataStore> ContextConfig { get; set; }
        #endregion

        #region Init
        private static RootDbInitializer _RootInitializer = null;
        static DataStore()
        {
            _RootInitializer = new RootDbInitializer();
            Database.SetInitializer(_RootInitializer);
        }


        public DataStore(ContextConfiguration<DataStore> configuration)
        : base(configuration.ConnectionString)
        {
            ContextConfig = configuration;

            _RootInitializer.RegisterInstanceInitializer(this, configuration.DatabaseInitializer);

            //configure EF. Note that only configuration actions should be carried out here.
            ContextConfig.EFContextConfiguration?.Invoke(Configuration);
        }
        #endregion

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            ContextConfig.ConfiguredModules.ForAll(_module => _module.ConfigureContext(modelBuilder));

            //lock module config providers
            ContextConfig.ConfiguredModules.ForAll(_module => _module.Lock());
        }
    }
}
