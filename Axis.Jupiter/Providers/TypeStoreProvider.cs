using static Axis.Luna.Extensions.ExceptionExtension;

using Axis.Proteus.Ioc;
using System;
using System.Linq;
using Axis.Jupiter.Contracts;
using Axis.Luna.Extensions;
using Axis.Jupiter.Configuration;
using System.Collections.Generic;

namespace Axis.Jupiter.Providers
{
    public class TypeStoreProvider
    {
        private readonly IServiceResolver _resolver;
        private readonly Dictionary<string, TypeStoreEntry> _entries = null;


        public TypeStoreProvider(IServiceResolver resolver, TypeStoreMap storeMap)
        {
            ThrowNullArguments(
                nameof(resolver).ObjectPair(resolver),
                nameof(storeMap).ObjectPair(storeMap));

            _resolver = resolver;
            _entries = storeMap
                .Entries()
                .ToDictionary(
                    entry => entry.TypeName,
                    entry => entry)
                ?? new Dictionary<string, TypeStoreEntry>();
        }

        public IStoreCommand CommandFor(string typeName)
        {
            var entry = EntryFor(typeName);

            var command = _resolver
                .Resolve(entry.CommandServiceType) 
                ?? throw new Exception($"Unregistered Store Command Type found: {entry.CommandServiceType.FullName}");

            return (command as IStoreCommand) ?? throw new Exception($"Invalid Store Command Type resolution");
        }

        public IStoreCommand CommandFor<TModel>() => CommandFor(typeof(TModel).FullName);

        public IStoreQuery QueryFor(string typeName)
        {
            var entry = EntryFor(typeName);

            if (entry.QueryServiceType == null)
                return null;

            var command = _resolver
                .Resolve(entry.QueryServiceType)
                ?? throw new Exception($"Unregistered Store Query Type found: {entry.QueryServiceType.FullName}");

            return (command as IStoreQuery) ?? throw new Exception($"Invalid Store Query Type resolution");
        }

        public IStoreQuery QueryFor<TModel>() => QueryFor(typeof(TModel).FullName);

        public TypeStoreEntry EntryFor(string typeName)
        {
            return _entries.TryGetValue(typeName, out var entry)
                ? entry
                : throw new Exception($"Unknown TypeId found: {typeName}");
        }

        internal TypeStoreEntry[] Entries() => _entries.Values.ToArray();
    }
}
