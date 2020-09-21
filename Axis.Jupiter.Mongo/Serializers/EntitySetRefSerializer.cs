using Axis.Jupiter.MongoDb.Converters;
using Axis.Jupiter.MongoDb.XModels;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Axis.Jupiter.MongoDb.Serializers
{
    public class EntitySetRefSerializer<TRefInstance, TRefKey, TSourceKey> : SerializerBase<EntitySetRef<TRefInstance, TRefKey, TSourceKey>>, IBsonDocumentSerializer
    where TRefInstance : IMongoEntity<TRefKey>
    {
        #region JsonSerializerSettings
        private static readonly JsonSerializerSettings _JsonSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            Converters = new List<JsonConverter>
            {
                new EntitySetRefJsonConverter(),
                new SetRefEntityJsonConverter()
            },
        };
        #endregion


        public bool TryGetMemberSerializationInfo(string memberName, out BsonSerializationInfo serializationInfo)
        {
            serializationInfo = null;

            switch (memberName)
            {
                case nameof(IRefDbInfo.DbCollection):
                case nameof(IRefDbInfo.DbLabel):
                    serializationInfo = new BsonSerializationInfo(
                        memberName,
                        new StringSerializer(),
                        typeof(string));
                    return true;

                case nameof(IRefEnumerable.Refs):
                    serializationInfo = new BsonSerializationInfo(
                        memberName,
                        new ArraySerializer<SetRefEntity<TRefInstance, TRefKey, TSourceKey>>(), 
                        typeof(List<SetRefEntity<TRefInstance, TRefKey, TSourceKey>>));
                    return true;

                default:
                    return false;
            }
        }

        public override EntitySetRef<TRefInstance, TRefKey, TSourceKey> Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var bsonDocSerializer = BsonSerializer.LookupSerializer(typeof(BsonDocument));
            var json = bsonDocSerializer
                .Deserialize(context, args)
                .ToBsonDocument()
                .ToJson();

            return JsonConvert.DeserializeObject<EntitySetRef<TRefInstance, TRefKey, TSourceKey>>(json, _JsonSettings);
        }

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, EntitySetRef<TRefInstance, TRefKey, TSourceKey> value)
        {
            var jsonDocument = JsonConvert.SerializeObject(value, _JsonSettings);
            var bsonDocument = BsonSerializer.Deserialize<BsonDocument>(jsonDocument);

            var serializer = BsonSerializer.LookupSerializer(typeof(BsonDocument));
            serializer.Serialize(context, bsonDocument.AsBsonValue);
        }
    }
}
