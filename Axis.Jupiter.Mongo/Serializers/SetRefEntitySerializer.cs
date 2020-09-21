using Axis.Jupiter.MongoDb.Converters;
using Axis.Jupiter.MongoDb.XModels;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Axis.Jupiter.MongoDb.Serializers
{
    public class SetRefEntitySerializer<TRefInstance, TRefKey, TSourceKey> : SerializerBase<SetRefEntity<TRefInstance, TRefKey, TSourceKey>>, IBsonDocumentSerializer
    where TRefInstance : IMongoEntity<TRefKey>
    {
        #region JsonSerializerSettings
        private static readonly JsonSerializerSettings _JsonSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            Converters = new List<JsonConverter>
            {
                new SetRefEntityJsonConverter()
            },
        };
        #endregion


        public bool TryGetMemberSerializationInfo(string memberName, out BsonSerializationInfo serializationInfo)
        {
            serializationInfo = null;

            switch (memberName)
            {
                case nameof(ISetRef.Key):
                    serializationInfo = new BsonSerializationInfo(
                        memberName,
                        new GuidSerializer(),
                        typeof(Guid));
                    return true;

                case nameof(ISetRef.RefKey):
                    serializationInfo = new BsonSerializationInfo(
                        memberName,
                        BsonSerializer.LookupSerializer<TRefKey>(),
                        typeof(TRefKey));
                    return true;

                case nameof(ISetRef.RefLabel):
                    serializationInfo = new BsonSerializationInfo(
                        memberName,
                        new StringSerializer(),
                        typeof(string));
                    return true;

                case nameof(ISetRef.SourceKey):
                    serializationInfo = new BsonSerializationInfo(
                        memberName,
                        BsonSerializer.LookupSerializer<TSourceKey>(),
                        typeof(TSourceKey));
                    return true;

                default:
                    return false;
            }
        }

        public override SetRefEntity<TRefInstance, TRefKey, TSourceKey> Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var bsonDocSerializer = BsonSerializer.LookupSerializer(typeof(BsonDocument));
            var json = bsonDocSerializer
                .Deserialize(context, args)
                .ToBsonDocument()
                .ToJson();

            return JsonConvert.DeserializeObject<SetRefEntity<TRefInstance, TRefKey, TSourceKey>>(json, _JsonSettings);
        }

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, SetRefEntity<TRefInstance, TRefKey, TSourceKey> value)
        {
            var jsonDocument = JsonConvert.SerializeObject(value, _JsonSettings);
            var bsonDocument = BsonSerializer.Deserialize<BsonDocument>(jsonDocument);

            var serializer = BsonSerializer.LookupSerializer(typeof(BsonDocument));
            serializer.Serialize(context, bsonDocument.AsBsonValue);
        }
    }
}
