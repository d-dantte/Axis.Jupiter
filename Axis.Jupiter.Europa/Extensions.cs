using Axis.Jupiter.Europa.Mappings;
using Axis.Luna.Extensions;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Data.Entity.Infrastructure.Annotations;
using System.Data.Entity.ModelConfiguration;
using System.Data.Entity.ModelConfiguration.Configuration;
using System.Linq;
using System.Reflection;

namespace Axis.Jupiter.Europa
{
    public static class Extensions
    {
        public static PropConfig IsIndex<PropConfig>(this PropConfig property, string indexName, bool isUnique = false)
        where PropConfig: PrimitivePropertyConfiguration
        => property.HasColumnAnnotation("Index",  
                                        new IndexAnnotation(new IndexAttribute($"IX_{indexName.ThrowIf(n => string.IsNullOrWhiteSpace(n),"Invalid index name")}") { IsUnique = isUnique }))
                   .Cast<PropConfig>();

        private static string RandomIndexName() => Luna.RandomAlphaNumericGenerator.RandomAlpha(10);


        internal static EntityTypeConfiguration<EType> MapToDefaultTable<EType>(this EntityTypeConfiguration<EType> config)
        where EType : class => config.Map(m => m.ToTable(typeof(EType).Name));
        
        public static bool IsEntityMap(this Type t)
        => GetBaseMap(t).Pipe(bt => bt != null && bt != t && !t.IsInterface && !t.IsAbstract && !t.IsGenericType);

        public static bool IsComplexMap(this Type t)
        => GetBaseComplexMap(t).Pipe(bt => bt != null && bt != t && !t.IsInterface && !t.IsAbstract && !t.IsGenericType);


        #region private stuff
        internal static bool PropertiesAreEquivalent(PropertyInfo first, PropertyInfo second)
        => first.Name == second.Name &&
           first.PropertyType == second.PropertyType &&
           first.DeclaringType == second.DeclaringType;

        public static Type BaseMapType(this Type t) => GetBaseMap(t)?.GetGenericArguments().First();

        public static Type GetBaseMap(Type t)
        => typeof(BaseMap<>).Pipe(bmt => t.BaseTypes().FirstOrDefault(type => type.IsGenericType && type.GetGenericTypeDefinition() == bmt));

        public static Type BaseComplexMapType(this Type t) => GetBaseComplexMap(t)?.GetGenericArguments().First();

        public static Type GetBaseComplexMap(Type t)
        {
            var bcmt = typeof(BaseComplexMap<>);
            var bt = t.BaseTypes().FirstOrDefault(type => type.IsGenericType && type.GetGenericTypeDefinition() == bcmt);
            return bt;
        }
        #endregion
    }
}
