using static Axis.Luna.Extensions.ExceptionExtension;

using Axis.Proteus.Ioc;
using System;
using Axis.Jupiter.Contracts;
using Axis.Jupiter.Models;
using Axis.Luna.Extensions;

namespace Axis.Jupiter.Services
{
    public class TypeStoreProvider
    {
        private readonly IServiceResolver _resolver;
        private readonly TypeStoreMap _storeMap;


        public TypeStoreProvider(IServiceResolver resolver, TypeStoreMap storeMap)
        {
            ThrowNullArguments(
                nameof(resolver).ObjectPair(resolver),
                nameof(storeMap).ObjectPair(resolver));

            _resolver = resolver;
            _storeMap = storeMap;
        }

        public IStoreCommand CommandFor(string typeId)
        {
            var entry = EntryFor(typeId);

            var command = _resolver
                .Resolve(entry.CommandServiceType) 
                ?? throw new Exception($"Unregistered Store Command Type found: {entry.CommandServiceType.FullName}");

            return (command as IStoreCommand) ?? throw new Exception($"Invalid Store Command Type resolution");
        }

        public IStoreQuery QueryFor(string typeId)
        {
            var entry = EntryFor(typeId);

            var command = _resolver
                .Resolve(entry.QueryServiceType) 
                ?? throw new Exception($"Unregistered Store Query Type found: {entry.QueryServiceType.FullName}");

            return (command as IStoreQuery) ?? throw new Exception($"Invalid Store Query Type resolution");
        }

        public TypeStoreEntry EntryFor(string typeId)
        {
            return _storeMap
                .Entry(typeId)
                .ThrowIfNull(new Exception($"Unknown TypeId found: {typeId}"));
        }

        internal TypeStoreEntry[] Entries() => _storeMap.Entries();
    }
}
