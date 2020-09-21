using Axis.Jupiter.MongoDb.Serializers;
using Axis.Jupiter.MongoDb.XModels;
using Axis.Luna.Extensions;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Axis.Jupiter.MongoDb.Providers
{
    /// <summary>
    /// Usually, only one instance of this should be created
    /// </summary>
    public class EntityInfoProvider
    {
        private static readonly MethodInfo CreateEntityInfoMethod = typeof(EntityInfoProvider)
            .GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
            .FirstOrDefault(method => method.Name == nameof(CreateSetEntityInfo));

        private readonly Dictionary<Type, IEntityInfo> _infoMap;

        public EntityInfoProvider(IEnumerable<IEntityInfo> infos = null)
        {
            //move all these individual things into different initialization methods
            _infoMap = infos?
                .SelectMany(info =>
                {
                    //Build
                    return EntityInfoProvider
                        .GetSetRefProperties(info.EntityType)
                        .Select(SynthesizeInfoGenerics)
                        .Select(generics => InvokeCreation(this, info, generics))
                        .Concatenate(info)
                        .Select(AssignProvider)
                        .ToArray();
                })
                .ToDictionary(info => info.EntityType, info => info)
                ?? new Dictionary<Type, IEntityInfo>();


            #region Register Ref Serializers
            var registerRefSerializerMethod = typeof(EntityInfoProvider)
                .GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
                .FirstOrDefault(method => method.Name == nameof(RegisterRefSerializer));

            var registerSetRefEntitySerilizerMethod = typeof(EntityInfoProvider)
                .GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
                .FirstOrDefault(method => method.Name == nameof(RegisterSetRefEntitySerializer));

            var registerEntitySetRefSerilizerMethod = typeof(EntityInfoProvider)
                .GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
                .FirstOrDefault(method => method.Name == nameof(RegisterEntitySetRefSerializer));

            foreach (var info in _infoMap.Values)
            {
                //entity set ref: because we already registered SetRefEntityInfos for all found SetRefEntities,
                //we can use that to set up our serializers.
                //also, take the opportunity to create a serialier for the EntitySetRef that points to it
                if (info.EntityType.TryGetGenericTypeDefinition(out var genTypeDef)
                    && genTypeDef == (typeof(SetRefEntity<,,>)))
                {
                    //EntitySetRef
                    registerEntitySetRefSerilizerMethod
                        .MakeGenericMethod(info.EntityType.GetGenericArguments())
                        .CallStaticAction();

                    //SetRefEntity
                    registerSetRefEntitySerilizerMethod
                        .MakeGenericMethod(info.EntityType.GetGenericArguments())
                        .CallStaticAction();
                }

                //be generous and assume that every entity (except SetRefEntities) have other entities that
                //reference them via EntityRef; so we create cheap Serializers for those potential refs
                else
                {
                    //entity ref
                    registerRefSerializerMethod
                        .MakeGenericMethod(info.EntityType, info.KeyType)
                        .CallStaticAction();
                }
            }
            #endregion

            #region Register Entity Serializers
            #endregion

            #region Register Conventions
            //ConventionRegistry.Register(
            //    nameof(ImmutableObjectConvention),
            //    new ConventionPack { new ImmutableObjectConvention() },
            //    IsMongoModel);
            #endregion
        }


        public IEntityInfo[] Infos() => _infoMap.Values.ToArray();

        public IEntityInfo InfoForEntity<EntityType>()
        => _infoMap.TryGetValue(typeof(EntityType), out var value)
            ? value
            : null;

        public IEntityInfo InfoForEntity(Type entityType)
        => _infoMap.TryGetValue(entityType, out var value)
            ? value
            : null;

        public IEntityInfo InfoForSetEntity<TRefInstance, TRefKey, TSourceKey>()
        where TRefInstance : IMongoEntity<TRefKey>
        => InfoForSetEntity(typeof(TRefInstance), typeof(TRefKey), typeof(TSourceKey));

        public IEntityInfo InfoForSetEntity(
            Type refInstanceType,
            Type refKeyType,
            Type sourceKeyType)
        {
            return _infoMap
                .Values
                .FirstOrDefault(info =>
                {
                    var infoType = info.GetType();
                    if (infoType.TryGetGenericTypeDefinition(out var genTypeDef)
                        && genTypeDef == (typeof(SetRefEntityInfo<,,,>)))
                    {
                        var generics = infoType.GetGenericArguments();
                        return generics[0].Equals(refInstanceType)
                            && generics[1].Equals(refKeyType)
                            && generics[3].Equals(sourceKeyType);
                    }

                    else return false;
                });
        }

        private IEntityInfo AssignProvider(IEntityInfo entityInfo)
        {
            (entityInfo as IEntityProviderElement).Provider = this; //leaky abstraction i know...(cringing)
            return entityInfo;
        }

        private static void RegisterRefSerializer<TEntity, TKey>()
        where TEntity : IMongoEntity<TKey>, new()
        {
            MongoDB.Bson.Serialization.BsonSerializer.RegisterSerializer(
                typeof(EntityRef<TEntity, TKey>),
                new EntityRefSerializer<TEntity, TKey>());
        }

        private static void RegisterSetRefEntitySerializer<TRefInstance, TRefKey, TSourceKey>()
        where TRefInstance : IMongoEntity<TRefKey>, new()
        {
            MongoDB.Bson.Serialization.BsonSerializer.RegisterSerializer(
                typeof(SetRefEntity<TRefInstance, TRefKey, TSourceKey>),
                new SetRefEntitySerializer<TRefInstance, TRefKey, TSourceKey>());
        }

        private static void RegisterEntitySetRefSerializer<TRefInstance, TRefKey, TSourceKey>()
        where TRefInstance : IMongoEntity<TRefKey>, new()
        {
            MongoDB.Bson.Serialization.BsonSerializer.RegisterSerializer(
                typeof(EntitySetRef<TRefInstance, TRefKey, TSourceKey>),
                new EntitySetRefSerializer<TRefInstance, TRefKey, TSourceKey>());
        }


        private static SetRefEntityInfo<TRefInstance, TRefKey, TSourceInstance, TSourceKey> CreateSetEntityInfo<TRefInstance, TRefKey, TSourceInstance, TSourceKey>(
            string database, 
            EntityInfoProvider provider)
        where TRefInstance : IMongoEntity<TRefKey>
        where TSourceInstance : IMongoEntity<TSourceKey>
        {
            return new SetRefEntityInfo<TRefInstance, TRefKey, TSourceInstance, TSourceKey>(database, provider);
        }

        private static PropertyInfo[] GetSetRefProperties(Type type)
        {
            return type
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(IsEntitySetRef)
                .Where(IsNotIgnored)
                .ToArray();
        }

        private static Type[] SynthesizeInfoGenerics(PropertyInfo setRefProperty)
        {
            var srefGenerics = setRefProperty.PropertyType.GetGenericArguments();

            return new[]
            {
                srefGenerics[0],
                srefGenerics[1],
                setRefProperty.DeclaringType,
                srefGenerics[2]
            };
        }

        private static IEntityInfo InvokeCreation(EntityInfoProvider provider, IEntityInfo parent, Type[] generics)
        {
            var info = CreateEntityInfoMethod
                .MakeGenericMethod(generics)
                .CallStaticFunc(parent.Database, provider)
                .As<IEntityInfo>();

            return info;
        }

        private static bool IsEntitySetRef(PropertyInfo property)
        => property.PropertyType.TryGetGenericTypeDefinition(out var genTypeDef)
            && genTypeDef == (typeof(EntitySetRef<,,>));

        private static bool IsNotIgnored(PropertyInfo property)
        => property.GetCustomAttribute(typeof(BsonIgnoreAttribute)) == null
            && property.GetCustomAttribute(typeof(JsonIgnoreAttribute)) == null;
    }
}
