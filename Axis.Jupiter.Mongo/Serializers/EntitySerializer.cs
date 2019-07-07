using Axis.Jupiter.MongoDb.Models;
using Axis.Luna.Extensions;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Axis.Jupiter.MongoDb.Serializers
{
    public class EntitySerializer<TEntity, TKey>: SerializerBase<TEntity>, IBsonDocumentSerializer
    where TEntity: IMongoEntity<TKey>, new()
    {
        private Type EntityType => typeof(TEntity);

        public override TEntity Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var entity = new TEntity();

            var props = EntityProperties().ToDictionary(p => MemberName(p), p => p);
            object value = null;

            context.Reader.ReadStartDocument();
            while(context.Reader.State != BsonReaderState.EndOfDocument)
            {
                if (!props.TryGetValue(context.Reader.ReadName(), out var prop))
                {
                    //skip value so we are back at a name
                    context.Reader.SkipValue();
                    continue;
                }

                else if (IsGenericEnumerable(prop.PropertyType))
                {
                    var genericType = prop.PropertyType.GetGenericArguments()[0];
                    value = BsonSerializer
                        .LookupSerializer(ArrayTypeMethod(genericType).CallStaticFunc<Type>())
                        .Deserialize(context, args);
                }

                else if (IsNonGenericEnumerable(prop.PropertyType))
                    value = BsonSerializer
                        .LookupSerializer(typeof(object[]))
                        .Deserialize(context, args);

                //else if (IsDictionary(prop.PropertyType)){...}

                else
                    value = BsonSerializer
                        .LookupSerializer(prop.PropertyType)
                        .Deserialize(context, args);

                entity.SetPropertyValue(prop.Name, value);
            }
            context.Reader.ReadEndDocument();

            return entity;
        }

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TEntity value)
        {
            context.Writer.WriteStartDocument();

            EntityProperties().ForAll(prop =>
            {
                context.Writer.WriteName(MemberName(prop));

                if (IsGenericEnumerable(prop.PropertyType))
                {
                    var genericType = prop.PropertyType.GetGenericArguments()[0];
                    BsonSerializer
                        .LookupSerializer(ArrayTypeMethod(genericType).CallStaticFunc<Type>())
                        .Serialize(context, value.PropertyValue(prop.Name));
                }
                else if (IsNonGenericEnumerable(prop.PropertyType))
                {
                    BsonSerializer
                    .LookupSerializer(typeof(object[]))
                    .Serialize(context, value.PropertyValue(prop.Name));
                }
                //else if (IsDictionary(_p.PropertyType)){...}
                else
                {
                    var propertyValue = value.PropertyValue(prop.Name);

                    if (propertyValue == null)
                        context.Writer.WriteNull();

                    else
                        BsonSerializer
                            .LookupSerializer(prop.PropertyType)
                            .Serialize(context, value.PropertyValue(prop.Name));
                }
            });

            context.Writer.WriteEndDocument();
        }

        public bool TryGetMemberSerializationInfo(string memberName, out BsonSerializationInfo serializationInfo)
        {
            var property = typeof(TEntity).GetProperty(memberName);
            serializationInfo = null;

            if (property == null)
                return false;

            else
            {
                var elementName = MemberName(property);
                var serializer = MemberSerializer(property);

                serializationInfo = new BsonSerializationInfo(
                    elementName,
                    serializer,
                    property.PropertyType);

                return true;
            }
        }


        #region statics
        private static bool IsGenericEnumerable(Type type) => type.ImplementsGenericInterface(typeof(IEnumerable<>));

        private static bool IsNonGenericEnumerable(Type type) => type.Implements(typeof(System.Collections.IEnumerable));

        private static MethodInfo ArrayTypeMethod(Type genericType) 
        => EntitySerializerHelper.ArrayGenerators.GetOrAdd(
            genericType, 
            _gt => typeof(EntitySerializer<TEntity, TKey>).GetMethod(
                nameof(ArrayTypeFor),
                BindingFlags.Static | BindingFlags.NonPublic));

        private static Type ArrayTypeFor<TElement>() => typeof(TElement[]);

        private static bool IsNotIgnored(PropertyInfo pinfo) => !pinfo.IsDefined(typeof(BsonIgnoreAttribute));

        private static string MemberName(PropertyInfo property)
        => property.IsDefined(typeof(BsonElementAttribute))
            ? property.GetCustomAttribute<BsonElementAttribute>().ElementName
            : property.Name;

        private static PropertyInfo[] EntityProperties()
        => typeof(TEntity)
            .GetProperties()
            .Where(IsNotIgnored)
            .ToArray();

        private static IBsonSerializer MemberSerializer(PropertyInfo property)
        => !property.IsDefined(typeof(BsonSerializerAttribute))
            ? BsonSerializer.LookupSerializer(property.PropertyType)
            : (IBsonSerializer)Activator.CreateInstance(
                property.GetCustomAttribute<BsonSerializerAttribute>().SerializerType);

        private static readonly JsonSerializerSettings _JsonSettings = new JsonSerializerSettings
        {

        };

        private static readonly JsonWriterSettings _JsonWriterSettings = new JsonWriterSettings
        {
            GuidRepresentation = GuidRepresentation.CSharpLegacy,
            OutputMode = JsonOutputMode.Strict
        };
        #endregion
    }

    internal static class EntitySerializerHelper
    {
        internal static readonly ConcurrentDictionary<Type, MethodInfo> ArrayGenerators = new ConcurrentDictionary<Type, MethodInfo>();
    }
}
