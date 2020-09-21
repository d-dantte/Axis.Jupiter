﻿using Axis.Jupiter.MongoDb.Converters;
using Axis.Jupiter.MongoDb.XModels;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Axis.Jupiter.MongoDb.Serializers
{
    public class EntityRefSerializer<TEntity, TKey> : SerializerBase<EntityRef<TEntity, TKey>>, IBsonDocumentSerializer
    where TEntity : IMongoEntity<TKey>
    {
        #region JsonSerializerSettings
        private static readonly JsonSerializerSettings _JsonSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            Converters = new List<JsonConverter>
            {
                new EntityRefJsonConverter()
            },
        };
        #endregion


        public bool TryGetMemberSerializationInfo(string memberName, out BsonSerializationInfo serializationInfo)
        { 
            serializationInfo = null;

            switch (memberName)
            {
                case nameof(IEntityRef.DbLabel):
                case nameof(IEntityRef.DbCollection):
                    serializationInfo = new BsonSerializationInfo(
                        memberName,
                        new StringSerializer(),
                        typeof(string));
                    return true;

                case nameof(IRefIdentity.RefKey):
                    var property = typeof(TEntity).GetProperty(memberName);
                    var serializer = !property.IsDefined(typeof(BsonSerializerAttribute))
                        ? BsonSerializer.LookupSerializer(property.PropertyType)
                        : (IBsonSerializer)Activator.CreateInstance(
                            property.GetCustomAttribute<BsonSerializerAttribute>().SerializerType);

                    if (serializer == null)
                        return false;

                    serializationInfo = new BsonSerializationInfo(
                        memberName,
                        serializer,
                        typeof(TKey));
                    return true;

                default:
                    return false;
            }
        }

        public override EntityRef<TEntity, TKey> Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var bsonDocSerializer = BsonSerializer.LookupSerializer(typeof(BsonDocument));
            var json = bsonDocSerializer
                .Deserialize(context, args)
                .ToBsonDocument()
                .ToJson();

            return JsonConvert.DeserializeObject<EntityRef<TEntity, TKey>>(json, _JsonSettings);
        }

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, EntityRef<TEntity, TKey> value)
        {
            var jsonDocument = JsonConvert.SerializeObject(value, _JsonSettings);
            var bsonDocument = BsonSerializer.Deserialize<BsonDocument>(jsonDocument);

            var serializer = BsonSerializer.LookupSerializer(typeof(BsonDocument));
            serializer.Serialize(context, bsonDocument.AsBsonValue);
        }
    }
}