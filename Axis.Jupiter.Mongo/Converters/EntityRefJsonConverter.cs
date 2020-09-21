using Axis.Jupiter.MongoDb.XModels;
using Axis.Luna.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;

namespace Axis.Jupiter.MongoDb.Converters
{
    public class EntityRefJsonConverter : JsonConverter
    {
        private static readonly ConcurrentDictionary<Type, MethodInfo> _ActivatorCache = new ConcurrentDictionary<Type, MethodInfo>();

        public override bool CanConvert(Type objectType)
        {
            return objectType.IsGenericType
                && objectType.GetGenericTypeDefinition() == typeof(EntityRef<,>);
        }

        public override object ReadJson(
            JsonReader reader, 
            Type objectType, 
            object existingValue, 
            JsonSerializer serializer)
        {
            var jobject = JObject.Load(reader);

            var propertyName = nameof(IRefIdentity.RefKey);
            var key = jobject[propertyName]
                .ThrowIfNull(new Exception($"Invalid {propertyName}"))
                .ThrowIf(jt => jt.Type == JTokenType.Null, new Exception($"Invalid {propertyName}"));

            propertyName = nameof(IRefDbInfo.DbCollection);
            var collection = jobject[propertyName]
                .ThrowIfNull(new Exception($"Invalid {propertyName}"))
                .ThrowIf(jt => jt.Type == JTokenType.Null, new Exception($"Invalid {propertyName}"));

            propertyName = nameof(IRefDbInfo.DbLabel);
            var dblabel = jobject[propertyName];
            dblabel = dblabel.Type == JTokenType.Null ? null : dblabel;

            return _ActivatorCache
                .GetOrAdd(objectType, SynthesizeActivator)
                .CallStaticFunc(serializer, collection, dblabel, key);
        }

        public override void WriteJson(
            JsonWriter writer, 
            object value, 
            JsonSerializer serializer)
        {
            JToken
                .FromObject(value)
                .WriteTo(writer, serializer.Converters.ToArray());
        }


        private static EntityRef<TEntity, TKey> NewEntityRef<TEntity, TKey>(
            JsonSerializer serializer,
            JToken collection,
            JToken label,
            JToken key)
        where TEntity: IMongoEntity<TKey>
        {
            return new EntityRef<TEntity, TKey>(
                key.ToObject<TKey>(serializer),
                collection.ToObject<string>(serializer),
                label?.ToObject<string>(serializer));
        }

        private static MethodInfo SynthesizeActivator(Type refType)
        {
            var genericTypes = refType.GetGenericArguments();
            var method = typeof(EntityRefJsonConverter)
                .GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
                .FirstOrDefault(m => m.Name == nameof(NewEntityRef));

            return method.MakeGenericMethod(genericTypes);
        }
    }
}
