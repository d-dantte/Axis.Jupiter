﻿using System;
using Axis.Jupiter.Contracts;
using Axis.Jupiter.Models;
using Axis.Luna.Extensions;
using Axis.Proteus.Ioc;
using static Axis.Luna.Extensions.ExceptionExtension;

namespace Axis.Jupiter
{
    public class StoreProvider
    {
        private readonly IServiceResolver _resolver;
        private readonly TypeStoreMap _storeMap;


        public StoreProvider(IServiceResolver resolver, TypeStoreMap storeMap)
        {
            ThrowNullArguments(
                nameof(resolver).ObjectPair(resolver),
                nameof(storeMap).ObjectPair(storeMap));

            _resolver = resolver;
            _storeMap = storeMap;
        }

        public IStoreCommand CommandFor(string typeId)
        {
            var entry = _storeMap
                .Entry(typeId) 
                ?? throw new Exception($"Invalid Type Id: {typeId}");

            var command = _resolver
                .Resolve(entry.CommandServiceType) 
                ?? throw new Exception($"Unregistered Store Command Type found: {entry.CommandServiceType.FullName}");

            return (command as IStoreCommand) ?? throw new Exception($"Invalid Store Command Type resolution");
        }

        public IStoreQuery QueryFor(string typeId)
        {
            var entry = _storeMap
                .Entry(typeId) 
                ?? throw new Exception($"Invalid Type Id: {typeId}");

            var command = _resolver
                .Resolve(entry.QueryServiceType) 
                ?? throw new Exception($"Unregistered Store Query Type found: {entry.QueryServiceType.FullName}");

            return (command as IStoreQuery) ?? throw new Exception($"Invalid Store Query Type resolution");
        }
    }
}
