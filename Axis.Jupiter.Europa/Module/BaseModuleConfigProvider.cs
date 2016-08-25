using static Axis.Luna.Extensions.ObjectExtensions;
using static Axis.Luna.Extensions.ExceptionExtensions;
using static Axis.Luna.Extensions.EnumerableExtensions;

using System;
using System.Collections.Generic;
using System.Data.Entity.ModelConfiguration;
using System.Data.Entity;
using System.Reflection;
using System.Linq;

namespace Axis.Jupiter.Europa.Module
{
    public abstract class BaseModuleConfigProvider : IModuleConfigProvider
    {
        #region Properties
        private Dictionary<Type, dynamic> _entityConfigs { get; set; } = new Dictionary<Type, dynamic>();
        private Dictionary<Type, List<dynamic>> _entitySeeders { get; set; } = new Dictionary<Type, List<dynamic>>();
        private List<Action<IDataContext>> _contextActions { get; set; } = new List<Action<IDataContext>>();
        private List<Action<DbModelBuilder>> _modelBuilderActions { get; set; } = new List<Action<DbModelBuilder>>();
        private Dictionary<Type, dynamic> _storeQueryGenerators { get; set; } = new Dictionary<Type, dynamic>();
        private Dictionary<string, dynamic> _contextQueryGenerators { get; set; } = new Dictionary<string, dynamic>();
        private MethodInfo StoreMethod { get; set; } = typeof(EuropaContext).GetMethod(nameof(EuropaContext.Store));

        public abstract string ModuleName { get; }

        public bool IsLocked { get; private set; }

        public IEnumerable<KeyValuePair<Type, dynamic>> StoreQueryGenerators => _storeQueryGenerators.ToArray();
        public IEnumerable<KeyValuePair<string, dynamic>> ContextQueryGenerators => _contextQueryGenerators.ToArray();
        #endregion

        #region Methods

        public void Lock() => IsLocked = true;

        public IEnumerable<Type> ConfiguredTypes() => _entityConfigs.Keys;

        public IModuleConfigProvider UsingModelBuilder(Action<DbModelBuilder> action)
            => Try(() => _modelBuilderActions.Add(action.ThrowIfNull()));

        public IModuleConfigProvider UsingConfiguration<Entity>(EntityTypeConfiguration<Entity> configuration)
        where Entity : class => Try(() => _entityConfigs[typeof(Entity)] = configuration.ThrowIfNull());
        public IModuleConfigProvider UsingConfiguration<Entity>(ComplexTypeConfiguration<Entity> configuration)
        where Entity : class => Try(() => _entityConfigs[typeof(Entity)] = configuration.ThrowIfNull());

        public IModuleConfigProvider UsingEntitySeed<Entity>(Action<ObjectStore<Entity>> seeder)
        where Entity : class => Try(() => _entitySeeders.GetOrAdd(typeof(Entity), _k => new List<dynamic>()).Add(seeder.ThrowIfNull()));

        public IModuleConfigProvider UsingContext(Action<IDataContext> contextAction)
            => Try(() => _contextActions.Add(contextAction.ThrowIfNull()));

        public IModuleConfigProvider WithStoreQueryGenerator<Entity>(Func<IDataContext, IQueryable<Entity>> generator)
        where Entity : class => Try(() => _storeQueryGenerators.Add(typeof(Entity), generator.ThrowIfNull("null generator")));

        public IModuleConfigProvider WithContextQueryGenerator<Entity>(string queryIdentity, Func<IDataContext, object[], IQueryable<Entity>> generator)
        where Entity : class => Try(() => _contextQueryGenerators.Add(queryIdentity, generator.ThrowIfNull("null generator")));

        public void ConfigureContext(DbModelBuilder modelBuilder) => Try(() =>
        {
           //do general configurations
           _modelBuilderActions.ForEach(_mba => _mba.Invoke(modelBuilder));

           //do entity specific configurations
           _entityConfigs.Values.ForAll((cnt, next) => modelBuilder.Configurations.Add(next));
        });

        public void InitializeContext(EuropaContext context) => this.UsingValue(t =>
        {
            //run context actions first
            _contextActions.ForAll((cnt, next) => next.Invoke(context));
        
            //run entity seeders. I will deprecate this feature soonest.
            _entitySeeders.ForAll((cnt, next) =>
                {
                    var GenericStore = StoreMethod.MakeGenericMethod(next.Key);
                    dynamic store = GenericStore.Invoke(context, null);
                    next.Value.ForAll((__cnt, _next) => _next.Invoke(store));
                });
        });

        private IModuleConfigProvider Try(Action action)
        {
            IsLocked.ThrowIf(_locked => _locked, "Attempting to configure a locked config provider");

            action();
            return this;
        }

        #endregion

        #region Init
        protected BaseModuleConfigProvider()
        {
            Initialize();
        }

        /// <summary>
        /// This method is called from the constructor of this class to give the implementer the chance to populate the seeders and configs
        /// </summary>
        protected abstract void Initialize();
        #endregion
    }
}
