using Axis.Luna.Extensions;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Conventions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Axis.Jupiter.MongoDb.Conventions
{
    public class ImmutableObjectConvention : ConventionBase, IClassMapConvention
    {
        private readonly BindingFlags _bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public ImmutableObjectConvention()
        { }

        public void Apply(BsonClassMap classMap)
        {
            var readOnlyProperties = classMap.ClassType
                .GetTypeInfo()
                .GetProperties(_bindingFlags)
                .Where(p => IsReadOnlyOrExplicitProperty(classMap, p))
                .ToArray();

            if (readOnlyProperties.Length > 0)
            {
                foreach (var constructor in classMap.ClassType.GetConstructors())
                {
                    // If we found a matching constructor then we map it and all the readonly properties
                    if (MatchesParams(constructor, readOnlyProperties))
                    {
                        // Map constructor
                        classMap.MapConstructor(constructor);

                        // Map properties
                        foreach (var p in readOnlyProperties)
                            classMap.MapMember(p);
                    }
                }
            }
        }

        private static bool MatchesParams(
            ConstructorInfo constructor, 
            PropertyInfo[] readonlyProps)
        {
            var ctorParameters = constructor.GetParameters();
            return readonlyProps.ExactlyAll(prop => ctorParameters.Any(param =>
            {
                return string.Equals(prop.Name, param.Name, StringComparison.InvariantCultureIgnoreCase)
                    && prop.PropertyType.IsAssignableFrom(param.ParameterType);
            }));
        }

        private static bool IsReadOnlyOrExplicitProperty(BsonClassMap classMap, PropertyInfo propertyInfo)
        {
            if (propertyInfo.IsDefined(typeof(BsonIgnoreAttribute)))
                return false;

            // we can't read 
            if (!propertyInfo.CanRead)
                return false;

            // we can write (already handled by the default convention...)
            if (propertyInfo.CanWrite)
                return false;

            // skip indexers
            if (propertyInfo.GetIndexParameters().Length != 0)
                return false;

            // skip overridden properties (they are already included by the base class)
            // note that Explicit properties are not skipped because their declaring types
            // will be the same as the ClassType
            var getMethodInfo = propertyInfo.GetMethod;
            if (getMethodInfo.IsVirtual && getMethodInfo.GetBaseDefinition().DeclaringType != classMap.ClassType)
                return false;

            return true;
        }
    }
}
