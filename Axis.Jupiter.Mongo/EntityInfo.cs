using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Axis.Luna.Extensions;
using MongoDB.Driver;

namespace Axis.Jupiter.Mongo
{
    public class EntityInfo
    {
        public Type EntityType { get; set; }
        public string[] KeyProperties { get; set; }

        public FilterDefinition<Entity> IdentityFilter<Entity>(object[] values)
        {
            if(values.Length != KeyProperties.Length)
                throw new Exception("Key Value count does not match Key Property count");

            Func<FieldDefinition<Entity, object>, object, FilterDefinition<Entity>> @delegate = Builders<Entity>.Filter.Eq<object>;
            var eqMethod = @delegate.Method.GetGenericMethodDefinition();

            if (KeyProperties.Length == 1)
            {
                var keyProperty = EntityType
                    .GetProperty(KeyProperties[0])
                    .ThrowIfNull($"Invalid property name: {KeyProperties[0]}");

                var method = eqMethod.MakeGenericMethod(keyProperty.PropertyType);

                return Builders<Entity>
                    .Filter
                    .CallFunc<FilterDefinition<Entity>>(method, KeyProperties[0], values[0]);
            }

            else
                return KeyProperties
                    .PairWith(values)
                    .Select(pair =>
                    {
                        var keyProperty = EntityType
                            .GetProperty(pair.Key)
                            .ThrowIfNull($"Invalid property name: {pair.Key}");


                        var method = eqMethod.MakeGenericMethod(keyProperty.PropertyType);

                        return Builders<Entity>
                            .Filter
                            .CallFunc<FilterDefinition<Entity>>(method, pair.Key, pair.Value);
                    })
                    .ToArray()
                    .Pipe(Builders<Entity>.Filter.And);
        }
    }
}
