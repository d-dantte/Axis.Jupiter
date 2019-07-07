using Axis.Jupiter.MongoDb.Conventions;
using Axis.Jupiter.MongoDb.Models;
using Axis.Luna.Extensions;
using MongoDB.Bson.Serialization.Conventions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Axis.Jupiter.MongoDb.Providers
{
    /// <summary>
    /// Usually, only one instance of this should be created
    /// </summary>
    public class EntityInfoProvider
    {
        private readonly Dictionary<Type, IEntityInfo> _infoMap;

        public EntityInfoProvider(IEnumerable<IEntityInfo> infos = null)
        {
            _infoMap = infos?
                .ToDictionary(info => info.EntityType, info => info)
                ?? new Dictionary<Type, IEntityInfo>();

            //register all serializers
            //foreach(var info in _infoMap.Values)
            //{
            // MongoDB.Bson.Serialization.BsonSerializer.RegisterSerializer(info.EntityType, info.Serializer);
            //}

            ConventionRegistry.Register(
                nameof(ImmutableObjectConvention),
                new ConventionPack { new ImmutableObjectConvention() },
                IsMongoModel);
        }


        public IEntityInfo[] Infos() => _infoMap.Values.ToArray();

        public IEntityInfo InfoFor<EntityType>()
        => _infoMap.TryGetValue(typeof(EntityType), out var value)
            ? value
            : null;

        public IEntityInfo InfoFor(Type entityType)
        => _infoMap.TryGetValue(entityType, out var value)
            ? value
            : null;

        private bool IsMongoModel(Type type)
        => type.Implements(typeof(IMongoEntity))
            || type.Implements(typeof(IEntityRef))
            || type.Implements(typeof(IEntityCollectionRef));
    }
}
