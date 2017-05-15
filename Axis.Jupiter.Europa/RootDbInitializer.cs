using Axis.Luna.Extensions;
using System.Collections.Concurrent;
using System.Data.Entity;

namespace Axis.Jupiter.Europa
{
    public class RootDbInitializer: IDatabaseInitializer<DataStore>
    {
        private ConcurrentDictionary<DataStore, IDatabaseInitializer<DataStore>> _registeredInitializers = new ConcurrentDictionary<DataStore, IDatabaseInitializer<DataStore>>();


        internal void RegisterInstanceInitializer(DataStore storeInstance, IDatabaseInitializer<DataStore> instanceInitializer)
        => _registeredInitializers.GetOrAdd(storeInstance, _instance => instanceInitializer);

        public void InitializeDatabase(DataStore context)
        {
            IDatabaseInitializer<DataStore> initializer;
            _registeredInitializers.TryGetValue(context, out initializer).ThrowIf(_ => _, "The context has no registered initializers");

            initializer?.InitializeDatabase(context);

            //module initializations
            context.ContextConfig.ConfiguredModules.ForAll(module => module.InitializeContext(context));
            context.SaveChanges();
        }
    }
}
