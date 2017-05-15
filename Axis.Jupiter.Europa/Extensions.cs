using Axis.Jupiter.Europa.Mappings;
using Axis.Jupiter.Europa.Utils;
using Axis.Luna.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure.Annotations;
using System.Data.Entity.ModelConfiguration;
using System.Data.Entity.ModelConfiguration.Configuration;
using System.Data.SqlClient;
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


        public static BulkCopyOperation ToBulkCopyPayload<Context, Entity>(this ContextConfiguration<Context> configuration, SqlBulkCopyOptions options, params Entity[] entityList)
        where Context : DataStore where Entity : class => ToBulkCopyPayload(configuration, options, entityList.AsEnumerable());

        public static BulkCopyOperation ToBulkCopyPayload<Context, Entity>(this ContextConfiguration<Context> configuration, SqlBulkCopyOptions options, IEnumerable<Entity> entityList)
        where Context : DataStore
        where Entity : class
        {
            var builder = new DbModelBuilder(DbModelBuilderVersion.Latest);
            var model = builder.Build(new SqlConnection(configuration.ConnectionString));
            var efMappings = new EFMappings(model);

            var bco = new BulkCopyOperation();
            var tentity = typeof(Entity);
            tentity
                .GetProperties()

                //project an object representing properties and their corresponding metadata
                .Select(_pinfo => new
                {
                    Property = _pinfo,
                    EFModel = efMappings.MappingFor(_pinfo.DeclaringType), //<-- In a TPH scenario, different properties may be mapped to different tables
                    PropertyMap = efMappings.MappingFor(_pinfo.DeclaringType).Properties
                                            .First(_p => PropertiesAreEquivalent(_p.ClrProperty, _pinfo))
                })

                //group the property metadata by the table they map to
                .GroupBy(_pmap => _pmap.EFModel.MappedTable)
                .ForAll(_tmap =>
                {
                    var tmapar = _tmap.ToArray();
                    
                    //create bulk copy context
                    var _bcxt = new SqlBulkCopy(configuration.ConnectionString, options);
                    _bcxt.DestinationTableName = _tmap.Key;
                    bco.PayloadMap[_bcxt] = new DataTable { TableName = _tmap.Key };

                    //map the columns on the context
                    foreach (var _prop in tmapar)
                        if (_prop.PropertyMap.Key != PropertyModel.KeyMode.StoreGenerated)
                            _bcxt.ColumnMappings.Add(_prop.PropertyMap.MappedProperty, _prop.PropertyMap.MappedProperty);
                    
                    var columnsAreMapped = false;

                    //populate the datatable
                    foreach (var item in entityList)
                    {
                        var values = new List<object>();
                        tmapar.Where(_prop => _prop.PropertyMap.Key != PropertyModel.KeyMode.StoreGenerated).ForAll(_prop =>
                        {
                            var prop = _prop.PropertyMap;
                            if (!columnsAreMapped)
                                bco.PayloadMap[_bcxt].Columns.Add(prop.MappedProperty, Nullable.GetUnderlyingType(prop.ClrProperty.PropertyType) ?? prop.ClrProperty.PropertyType);

                            values.Add(prop.ClrProperty.GetValue(item)); //<-- this should be optimized - proly by caching a delegate to the getter function
                        });

                        columnsAreMapped = true;
                        bco.PayloadMap[_bcxt].Rows.Add(values.ToArray());
                    }
                });

            return bco;
        }


        #region private stuff
        private static bool PropertiesAreEquivalent(PropertyInfo first, PropertyInfo second)
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
