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
    public class SetRefEntityJsonConverter : JsonConverter
    {
        private static readonly ConcurrentDictionary<Type, MethodInfo> _ActivatorCache = new ConcurrentDictionary<Type, MethodInfo>();

        public override bool CanConvert(Type objectType)
        {
            return objectType.IsGenericType
                && objectType.GetGenericTypeDefinition() == typeof(SetRefEntity<,,>);
        }

        public override object ReadJson(
            JsonReader reader,
            Type objectType,
            object existingValue,
            JsonSerializer serializer)
        {
            var jobject = JObject.Load(reader);

            var propertyName = nameof(IRefIdentity.RefKey);
            var refKey = jobject[propertyName]
                .ThrowIf(IsJTokenNull, new Exception($"Invalid {propertyName}"));

            propertyName = nameof(ISetRef.SourceKey);
            var sourceKey = jobject[propertyName]
                .ThrowIf(IsJTokenNull, new Exception($"Invalid {propertyName}"));

            propertyName = nameof(ISetRef.RefLabel);
            var refLabel = jobject[propertyName]
                .ThrowIf(IsJTokenNull, new Exception($"Invalid {propertyName}"));

            return _ActivatorCache
                .GetOrAdd(objectType, SynthesizeActivator)
                .CallStaticFunc(serializer, sourceKey, refKey, refLabel);
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


        private SetRefEntity<TRefInstance, TRefKey, TSourceKey> NewEntityRef<TRefInstance, TRefKey, TSourceKey>(
            JsonSerializer serializer,
            JToken sourceKey,
            JToken refKey,
            JToken refLabel)
        where TRefInstance : IMongoEntity<TRefKey>
        {
            return new SetRefEntity<TRefInstance, TRefKey, TSourceKey>(
                sourceKey.ToObject<TSourceKey>(serializer),
                refKey.ToObject<TRefKey>(serializer),
                refLabel.ToObject<string>());
        }

        private static MethodInfo SynthesizeActivator(Type refType)
        {
            var genericTypes = refType.GetGenericArguments();
            var method = typeof(EntityRefJsonConverter)
                .GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
                .FirstOrDefault(m => m.Name == nameof(NewEntityRef));

            return method.MakeGenericMethod(genericTypes);
        }

        private static bool IsJTokenNull(JToken token)
        => token == null || token.Type == JTokenType.Null;
    }
}
