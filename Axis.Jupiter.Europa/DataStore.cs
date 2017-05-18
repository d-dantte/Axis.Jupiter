using static Axis.Jupiter.Europa.Extensions;

using Axis.Jupiter.Europa.Utils;
using Axis.Luna.Extensions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;

namespace Axis.Jupiter.Europa
{
    public class DataStore : DbContext, IDisposable
    {
        #region Properties
        internal ContextConfiguration<DataStore> ContextConfig { get; set; }
        internal Lazy<EFMappings> EFMappings { get; private set; }
        #endregion

        #region Init
        private static RootDbInitializer _RootInitializer = null;
        static DataStore()
        {
            _RootInitializer = new RootDbInitializer();
            Database.SetInitializer(_RootInitializer);
        }


        public DataStore(ContextConfiguration<DataStore> configuration)
        : base(configuration.ConnectionString)
        {
            ContextConfig = configuration;

            _RootInitializer.RegisterInstanceInitializer(this, configuration.DatabaseInitializer);

            //configure EF. Note that only configuration actions should be carried out here.
            ContextConfig.EFContextConfiguration?.Invoke(Configuration);

            //lazily create the mappings
            EFMappings = new Lazy<EFMappings>(() =>
            {
                var builder = new DbModelBuilder(DbModelBuilderVersion.Latest);
                ContextConfig.ConfiguredModules.ForAll(_module => _module.BuildModel(builder));
                var model = builder.Build(new SqlConnection(ContextConfig.ConnectionString));
                return new EFMappings(model);
            });
        }
        #endregion

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            ContextConfig.ConfiguredModules.ForAll(_module => _module.BuildModel(modelBuilder));
        }

        public TypeModel MappingFor<Entity>() =>  EFMappings.Value.MappingFor<Entity>();

        public BulkCopyOperation InsertBatch<Entity>(SqlBulkCopyOptions options, params Entity[] entityList)
        where Entity : class => InsertBatch(options, entityList.AsEnumerable());

        public BulkCopyOperation InsertBatch<Entity>(SqlBulkCopyOptions options, IEnumerable<Entity> entityList)
        where Entity : class
        {
            var builder = new DbModelBuilder(DbModelBuilderVersion.Latest);
            var model = builder.Build(new SqlConnection(this.ContextConfig.ConnectionString));
            var efMappings = this.EFMappings.Value;

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
                    var _bcxt = new SqlBulkCopy(this.ContextConfig.ConnectionString, options);
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
    }
}
