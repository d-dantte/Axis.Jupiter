
using Axis.Jupiter.Europa.Utils;
using Axis.Luna.Extensions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using Axis.Luna.Operation;
using System.Linq.Expressions;
using System.Collections.Concurrent;
using Axis.Jupiter.Commands;
using Axis.Jupiter.Query;

namespace Axis.Jupiter.Europa
{
    public class DataStore : DbContext, IPersistenceCommands, IEntityQuery, IDisposable
    {
        #region Properties
        private static ConcurrentDictionary<string, Action<object, object>> MutatorCache = new ConcurrentDictionary<string, Action<object, object>>();
        private static ConcurrentDictionary<string, Action<object, object>> HashSetAdderCache = new ConcurrentDictionary<string, Action<object, object>>();

        private bool IsListToTableFunctionPrepared = false;

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

        public IMapping MappingFor<Entity>() => EFMappings.Value.MappingFor<Entity>();

        public IMapping MappingFor(Type entityType) => EFMappings.Value.MappingFor(entityType);

        public BulkCopyOperation InsertBatch<Model>(SqlBulkCopyOptions options, params Model[] modelList)
        where Model : class => InsertBatch(options, modelList.AsEnumerable());

        public BulkCopyOperation InsertBatch<Model>(SqlBulkCopyOptions options, IEnumerable<Model> modelList, int batchSize = 0, Func<string, string> tableNameOverride = null)
        where Model : class
        {
            var builder = new DbModelBuilder(DbModelBuilderVersion.Latest);
            var model = builder.Build(new SqlConnection(this.ContextConfig.ConnectionString));
            var efMappings = EFMappings.Value;
            batchSize = Math.Abs(batchSize);

            var bco = new BulkCopyOperation();
            var converter = new ModelConverter(this);
            var emc = ContextConfig.ModelMapConfigFor<Model>().ThrowIfNull($"Model Map Config not found for: {typeof(Model).FullName}");

            //Get all tables that this entity may be mapped into - "tables" because in a TPT scenario, an entity may be shared into multiple tables
            efMappings
                .TableMappingsFor(emc.EntityType)
                .ForAll(_tmapping =>
                {
                    //create bulk copy context
                    var _bcxt = new SqlBulkCopyContext(new SqlConnection(ContextConfig.ConnectionString), options);
                    _bcxt.Context.DestinationTableName = tableNameOverride?.Invoke(_tmapping.Name) ?? _tmapping.Name;
                    _bcxt.Context.BatchSize = batchSize > 0 ? batchSize : _bcxt.Context.BatchSize;
                    bco.PayloadMap[_bcxt] = new DataTable { TableName = _tmapping.Name };

                    //map the columns on the context
                    var cols = _tmapping.ColumnModels.Where(_cm => _cm.MappedProperty.Key != ScalarPropertyMapping.KeyMode.StoreGenerated).ToArray();
                    foreach (var _prop in cols) 
                        _bcxt.Context.ColumnMappings.Add(_prop.MappedProperty.MappedColumn, _prop.MappedProperty.MappedColumn); //<-- dont include store generated ids

                    var columnsAreMapped = false;
                    
                    //populate the datatable
                    foreach (var item in modelList.Select(_model => converter.ToEntity(_model)))
                    {
                        var values = new List<object>();
                        cols.ForAll(_prop =>
                        {
                            var prop = _prop.MappedProperty;
                            if (!columnsAreMapped)
                                bco.PayloadMap[_bcxt].Columns.Add(prop.MappedColumn, Nullable.GetUnderlyingType(prop.ClrProperty.PropertyType) ?? prop.ClrProperty.PropertyType);

                            values.Add(item.PropertyValue(prop.ClrProperty.Name)); //<-- this should be optimized - proly by caching a delegate to the getter function
                        });

                        columnsAreMapped = true;
                        bco.PayloadMap[_bcxt].Rows.Add(values.ToArray());
                    }
                });

            return bco;
        }


        #region IPersistenceCommands for Models
        
        #region Add
        public IOperation<Model> Add<Model>(Model d)
        where Model : class => LazyOp.Try(() =>
        {
            var converter = new ModelConverter(this);
            var emc = ContextConfig.ModelMapConfigFor<Model>().ThrowIfNull($"Model Map Config not found for: {typeof(Model).FullName}");
            var entity = Set(emc.EntityType).Add(converter.ToEntity<Model>(d));
            SaveChanges();
            return converter.ToModel<Model>(entity);
        });

        public IOperation AddBatch<Model>(IEnumerable<Model> models, int batchSize = 0)
        where Model : class => LazyOp.Try(() =>
        {
            var bcpo = InsertBatch(SqlBulkCopyOptions.Default, models, batchSize);
            
            bcpo.Execute();
        });
        #endregion

        #region Delete
        public IOperation<Model> Delete<Model>(Model d)
        where Model : class => LazyOp.Try(() =>
        {
            var emc = ContextConfig.ModelMapConfigFor<Model>().ThrowIfNull($"Model Map Config not found for: {typeof(Model).FullName}");
            var set = Set(emc.EntityType);
            var local = GetLocally(d);
            var entity = local == null ?
                         set.Attach(d) :
                         local;

            entity = set.Remove(entity);
            SaveChanges();
            
            return new ModelConverter(this).ToModel<Model>(entity);
        });

        public IOperation DeleteBatch<Model>(IEnumerable<Model> models, int batchSize = 0)
        where Model : class => LazyOp.Try(() =>
        {
            PrepareListToTableFunction();

            var mappingConfig = ContextConfig.ModelMapConfigFor<Model>();
            EFMappings.Value
                .TableMappingsFor(mappingConfig.EntityType)

                //make sure this model has an entity with a simple primary key (key with one value).
                .Where(_tableMapping => _tableMapping.ColumnModels.Count(_sp => _sp.IsPrimaryKey) == 1) 

                //make sure dependent tables are deleted first. Dependent tables are those whose primary keys are also foreign keys
                .OrderByDescending(_tableMapping => _tableMapping.ColumnModels.First(_sp => _sp.IsPrimaryKey).IsForeignKey)

                //run delete commands
                .ForAll(_tableMapping =>
                {
                    var primaryKey = _tableMapping.ColumnModels.First(_p => _p.IsPrimaryKey);

                    //construct delete statement passing the keylist as a parameter
                    var deleteStatement = $"DELETE _target FROM {_tableMapping.Name} AS _target " +
                                          $"INNER JOIN (SELECT CONVERT({primaryKey.DbType}, str) AS ID FROM {Constants.SQL_Function_ListToTable}(@list, @delimiter)) AS _joinList " +
                                          $"ON _joinList.ID = _target.{primaryKey.Name};";

                    var converter = new ModelConverter(this);
                    batchSize = Math.Abs(batchSize);
                    models
                        .Batch(batchSize > 0 ? batchSize : int.MaxValue)
                        .ForAll(_batch =>
                        {
                            var keyList = _batch
                                .Select(_m => converter.ToEntity(_m))
                                .Select(_e => _e.PropertyValue(primaryKey.MappedProperty.ClrProperty.Name).ToString()) //use a property serialization for specific types of values
                                .JoinUsing(",");
                            
                            //execute the statement
                            Database.ExecuteSqlCommand(deleteStatement, new SqlParameter("@list", keyList), new SqlParameter("@delimiter", ",")); //<-- ignore the result (for now)
                        });
                });
        });
        #endregion

        #region Update
        public IOperation<Model> Update<Model>(Model model)
        where Model : class => LazyOp.Try(() =>
        {
            var emc = ContextConfig.ModelMapConfigFor<Model>().ThrowIfNull($"Model Map Config not found for: {typeof(Model).FullName}");
            var converter = new ModelConverter(this);
            var entity = converter.ToEntity(model);
            var local = GetLocally(entity);

            //if the entity was found locally, copy from the supplied object
            if (local != null)
                local.CopyFrom(entity);

            //if the entity wasn't found locally, simply attach it
            else
                Set(emc.EntityType).Attach(local = model);

            Entry(local).State = EntityState.Modified;

            SaveChanges();

            return new ModelConverter(this).ToModel<Model>(local);
        });

        public IOperation UpdateBatch<Model>(IEnumerable<Model> models, int batchSize = 0)
        where Model : class => LazyOp.Try(() =>
        {
            ///use bulk insert to push the values into a #TEMPORARY table on the db, then another sql statement to copy values from the temp table into the actual table.
            var mappingConfig = ContextConfig.ModelMapConfigFor<Model>();
            var entityMapping = MappingFor(mappingConfig.EntityType) as TypeMapping;
            
            //bulk copy values into the table
            var converter = new ModelConverter(this);
            batchSize = Math.Abs(batchSize);
            models
                .Batch(batchSize > 0 ? batchSize : int.MaxValue)
                .ForAll(_batch =>
                {
                    //build bulkcopy operations
                    var bcpo = InsertBatch(SqlBulkCopyOptions.Default, _batch, 0, _tname => $"#{_tname}");

                    #region execute before bulk copy
                    bcpo.PreExecute = _cxt =>
                    {
                        //create the temporary table that will be bulk-inserted into
                        var stmt = $"SELECT * INTO {_cxt.Context.DestinationTableName} FROM {_cxt.Context.DestinationTableName.TrimStart("#")} WHERE 1 = 0;";

                        var command = _cxt.Connection.CreateCommand();
                        command.CommandText = stmt;
                        command.ExecuteNonQuery();
                    };
                    #endregion

                    #region execute after bulk copy
                    bcpo.PostExecute = _cxt =>
                    {
                        var _tm = EFMappings.Value.TableMappings.First(_x => _x.Name == _cxt.Context.DestinationTableName.TrimStart("#"));

                        //generate update statement per table
                        var newUpdate = $"UPDATE _{_tm.Name}";

                        //aggregate and add the "SET" commands
                        newUpdate += "\n SET "+ _tm.ColumnModels
                                .Where(_cm => !_cm.IsPrimaryKey)
                                .Aggregate("", (__stmt, _cm) => $"{__stmt}\n _{_tm.Name}.{_cm.Name} = __{_tm.Name}.{_cm.Name},")
                                .Pipe(__stmt => __stmt.TrimEnd(","));

                        //FROM
                        newUpdate += $"\n FROM {_tm.Name} AS _{_tm.Name}";

                        //INNER JOIN
                        newUpdate += $"\n INNER JOIN #{_tm.Name} AS __{_tm.Name}";

                        //ON primarykey = #primarykey
                        var pkey = _tm.ColumnModels.First(_cm => _cm.IsPrimaryKey);
                        newUpdate += $"\n ON _{_tm.Name}.{pkey.Name} = __{_tm.Name}.{pkey.Name};";

                        var command = _cxt.Connection.CreateCommand();
                        command.CommandText = newUpdate;
                        command.ExecuteNonQuery();
                    };
                    #endregion

                    using (bcpo) bcpo.Execute();
                });
        });
        #endregion

        #endregion

        #region IQueryCommands for Entities

        public IQueryable<Entity> Query<Entity>(params Expression<Func<Entity, object>>[] includes) 
        where Entity : class => includes.Aggregate(Set<Entity>() as IQueryable<Entity>, (_acc, _next) => _acc.Include(_next));

        #endregion
        
        #region Misc

        internal Entity GetLocally<Entity>(Entity entity)
        where Entity : class
        {
            //get the object keys. This CAN be cached, but SHOULD it be?
            var keys = this
                .MappingFor<Entity>()
                .ScalarProperties
                .Where(_p => _p.IsKey)
                .Select(_p => _p.ClrProperty.Name.ValuePair(entity.PropertyValue(_p.ClrProperty.Name)));

            //find the entity locally
            return Set<Entity>().Local.FirstOrDefault(_e =>
            {
                return keys.Select(_k => _e.PropertyValue(_k.Key))
                           .SequenceEqual(keys.Select(_k => _k.Value));
            });
        }

        internal object GetLocally(object entity) => GetLocally(entity.GetType(), entity);

        internal object GetLocally(Type entityType, object entity)
        => GetLocally(
            Set(entityType),
            //get the object keys. This CAN be cached, but SHOULD it be?
            MappingFor(entityType)
                .ScalarProperties
                .Where(_p => _p.IsKey)
                .Select(_p => _p.ClrProperty.Name.ValuePair(entity.PropertyValue(_p.ClrProperty.Name)))
                .ToArray(),
            entity);

        internal object GetLocally(DbSet set, KeyValuePair<string, object>[] keys, object entity)
        => set.Local.Cast<object>().FirstOrDefault(_e =>
        {
            return keys.Select(_k => _e.PropertyValue(_k.Key))
                       .SequenceEqual(keys.Select(_k => _k.Value));
        });


        private void PrepareListToTableFunction()
        {
            if (!IsListToTableFunctionPrepared)
            {
                var checkFunction = $"SELECT OBJECT_ID('dbo.{Constants.SQL_Function_ListToTable}')";
                if (Database.SqlQuery<int?>(checkFunction).FirstOrDefault() == null)
                {
                    #region Create Function Statement
                    var createFunction = @"
CREATE FUNCTION listToTable
                 (@list      nvarchar(MAX),
                  @delimiter nchar(1) = N',')
      RETURNS @tbl TABLE (listpos int IDENTITY(1, 1) NOT NULL,
                          str     varchar(4000)      NOT NULL) AS

BEGIN
   DECLARE @endpos   int,
           @startpos int,
           @textpos  int,
           @chunklen smallint,
           @tmpstr   nvarchar(4000),
           @leftover nvarchar(4000),
           @tmpval   nvarchar(4000)

   SET @textpos = 1
   SET @leftover = ''
   WHILE @textpos <= datalength(@list) / 2
   BEGIN
      SET @chunklen = 4000 - datalength(@leftover) / 2
      SET @tmpstr = @leftover + substring(@list, @textpos, @chunklen)
      SET @textpos = @textpos + @chunklen

      SET @startpos = 0
      SET @endpos = charindex(@delimiter COLLATE Slovenian_BIN2, @tmpstr)

      WHILE @endpos > 0
      BEGIN
         SET @tmpval = ltrim(rtrim(substring(@tmpstr, @startpos + 1,
                                             @endpos - @startpos - 1)))
         INSERT @tbl (str) VALUES(@tmpval)
         SET @startpos = @endpos
         SET @endpos = charindex(@delimiter COLLATE Slovenian_BIN2,
                                 @tmpstr, @startpos + 1)
      END

      SET @leftover = right(@tmpstr, datalength(@tmpstr) / 2 - @startpos)
   END

   INSERT @tbl(str)
      VALUES (ltrim(rtrim(@leftover)))
   RETURN
END
";
                    #endregion
                    Database.ExecuteSqlCommand(createFunction);

                    IsListToTableFunctionPrepared = true;
                }
            }
        }
        #endregion
    }
}
