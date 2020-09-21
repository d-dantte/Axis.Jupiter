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
    public class EntitySetRefJsonConverter : JsonConverter
    {
        private static readonly ConcurrentDictionary<Type, MethodInfo> _ActivatorCache = new ConcurrentDictionary<Type, MethodInfo>();

        public override bool CanConvert(Type objectType)
        {
            return objectType.IsGenericType
                && objectType.GetGenericTypeDefinition() == typeof(EntitySetRef<,,>);
        }

        public override object ReadJson(
            JsonReader reader, 
            Type objectType, 
            object existingValue, 
            JsonSerializer serializer)
        {
            var jobject = JObject.Load(reader);

            var propertyName = nameof(IRefEnumerable.Refs);
            var refs = jobject[propertyName];

            propertyName = nameof(IRefDbInfo.DbCollection);
            var collection = jobject[propertyName]
                .ThrowIfNull(new Exception($"Invalid {propertyName}"))
                .ThrowIf(jt => jt.Type == JTokenType.Null, new Exception($"Invalid {propertyName}"));

            propertyName = nameof(IRefDbInfo.DbLabel);
            var dblabel = jobject[propertyName];
            dblabel = dblabel.Type == JTokenType.Null ? null : dblabel;

            return _ActivatorCache
                .GetOrAdd(objectType, SynthesizeActivator)
                .CallStaticFunc(serializer, collection, dblabel, refs);
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


        private static EntitySetRef<TRefInstance, TRefKey, TSourceKey> NewEntitySetRef<TRefInstance, TRefKey, TSourceKey>(
            JsonSerializer serializer,
            JToken collection,
            JToken label,
            JToken refs)
        where TRefInstance : IMongoEntity<TRefKey>
        {
            var refInstances = refs == null || refs.Type == JTokenType.Null
                ? new SetRefEntity<TRefInstance, TRefKey, TSourceKey>[0]
                : refs.ToObject<SetRefEntity<TRefInstance, TRefKey, TSourceKey>[]>(serializer);

            return new EntitySetRef<TRefInstance, TRefKey, TSourceKey>(
                collection.ToObject<string>(serializer),
                label?.ToObject<string>(serializer),
                refInstances);
        }

        private static MethodInfo SynthesizeActivator(Type refType)
        {
            var genericTypes = refType.GetGenericArguments();
            var method = typeof(EntitySetRefJsonConverter)
                .GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
                .FirstOrDefault(m => m.Name == nameof(NewEntitySetRef));

            return method.MakeGenericMethod(genericTypes);
        }
    }
}
