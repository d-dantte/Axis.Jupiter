using static Axis.Luna.Extensions.ExceptionExtensions;
using static Axis.Luna.Extensions.EnumerableExtensions;

using System;
using System.Collections.Generic;
using System.Data.Entity.ModelConfiguration;
using System.Data.Entity;
using System.Linq;

namespace Axis.Jupiter.Europa.Module
{
    public class ModuleConfigProvider : IModuleConfigProvider
    {
        #region Properties
        private Dictionary<Type, dynamic> _entityConfigs { get; set; } = new Dictionary<Type, dynamic>();
        private List<Action<DataStore>> _contextActions { get; set; } = new List<Action<DataStore>>();
        private List<Action<DbModelBuilder>> _modelBuilderActions { get; set; } = new List<Action<DbModelBuilder>>();

        public string ModuleName { get; private set; }
        public bool IsLocked { get; private set; }
        #endregion

        #region Methods

        private void Lock() => IsLocked = true;

        public IEnumerable<Type> ConfiguredTypes() => _entityConfigs.Keys.ToArray();

        public IModuleConfigProvider UsingModelBuilder(Action<DbModelBuilder> action)
        => Try(() => _modelBuilderActions.Add(action.ThrowIfNull()));

        public IModuleConfigProvider UsingConfiguration<Entity>(EntityTypeConfiguration<Entity> configuration)
        where Entity : class => Try(() => _entityConfigs[typeof(Entity)] = configuration.ThrowIfNull());
        public IModuleConfigProvider UsingConfiguration<Entity>(ComplexTypeConfiguration<Entity> configuration)
        where Entity : class => Try(() => _entityConfigs[typeof(Entity)] = configuration.ThrowIfNull());

        public IModuleConfigProvider WithContextAction(Action<DataStore> contextAction)
        => Try(() => _contextActions.Add(contextAction.ThrowIfNull()));

        /// <summary>
        /// Called during DbContext.OnModelCreating
        /// </summary>
        /// <param name="modelBuilder"></param>
        public void BuildModel(DbModelBuilder modelBuilder)
        {
            Lock();

           //do general configurations
           _modelBuilderActions.ForEach(_mba => _mba.Invoke(modelBuilder));

           //do entity specific configurations
           _entityConfigs.Values.ForAll(next => modelBuilder.Configurations.Add(next));
        }

        /// <summary>
        /// Called by DataStore upon initialization
        /// </summary>
        /// <param name="context"></param>
        public void InitializeContext(DataStore context)
        {
            Lock();

            //run context actions first
            _contextActions.ForAll((cnt, next) => next.Invoke(context));
        }

        private IModuleConfigProvider Try(Action action)
        {
            IsLocked.ThrowIf(_locked => _locked, "Attempting to configure a locked config provider");

            action();
            return this;
        }

        #endregion
        
        public ModuleConfigProvider(string name = null)
        {
            ModuleName = string.IsNullOrWhiteSpace(name) ? 
                         Guid.NewGuid().ToString() :
                         name;
        }
    }
}