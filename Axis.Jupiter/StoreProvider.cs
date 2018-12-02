using System;
using Axis.Jupiter.Contracts;
using Axis.Proteus.Ioc;
using static Axis.Luna.Extensions.ExceptionExtension;

namespace Axis.Jupiter
{
    public class StoreProvider
    {
        private readonly IServiceResolver _resolver;
        private readonly StoreMap _storeMap;


        public StoreProvider(IServiceResolver resolver, StoreMap storeMap)
        {
            ThrowNullArguments(
                () => resolver,
                () => storeMap);

            _resolver = resolver;
            _storeMap = storeMap;
        }

        public IStoreCommand CommandFor(string storeId)
        {
            var entry = _storeMap.StoreEntry(storeId) ?? _storeMap.Default;
            var command = _resolver.Resolve(entry.StoreCommandType) ?? throw new Exception($"Unregistered Store Command Type found: {entry.StoreCommandType.FullName}");
            return (command as IStoreCommand) ?? throw new Exception($"Invalid Store Command Type resolution");
        }

        public IStoreQuery QueryFor(string storeId)
        {
            var entry = _storeMap.StoreEntry(storeId) ?? _storeMap.Default;
            var command = _resolver.Resolve(entry.StoreQueryType) ?? throw new Exception($"Unregistered Store Query Type found: {entry.StoreQueryType.FullName}");
            return (command as IStoreQuery) ?? throw new Exception($"Invalid Store Query Type resolution");
        }

        public IStoreCommand DefaultStoreCommand()
        {
            var entry = _storeMap.Default ?? throw new Exception($"No Default Entry found");
            var command = _resolver.Resolve(entry.StoreCommandType) ?? throw new Exception($"Unregistered Store Command Type found: {entry.StoreCommandType.FullName}");
            return (command as IStoreCommand) ?? throw new Exception($"Invalid Store Command Type resolution");
        }

        public IStoreQuery DefaultStoreQuery()
        {
            var entry = _storeMap.Default ?? throw new Exception($"No Default Entry found");
            var command = _resolver.Resolve(entry.StoreQueryType) ?? throw new Exception($"Unregistered Store Query Type found: {entry.StoreQueryType.FullName}");
            return (command as IStoreQuery) ?? throw new Exception($"Invalid Store Query Type resolution");
        }
    }
}