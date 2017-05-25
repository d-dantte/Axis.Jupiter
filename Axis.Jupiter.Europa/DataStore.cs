using static Axis.Jupiter.Europa.Extensions;

using Axis.Jupiter.Europa.Utils;
using Axis.Luna.Extensions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using Axis.Jupiter.Kore.Commands;
using Axis.Luna.Operation;
using System.Linq.Expressions;
using System.Collections.Concurrent;
using System.Reflection;

namespace Axis.Jupiter.Europa
{
    public class DataStore : DbContext, IPersistenceCommands, IQueryCommands, IDisposable
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

        public TypeModel MappingFor<Entity>() => EFMappings.Value.MappingFor<Entity>();
        public TypeModel MappingFor(Type entityType) => EFMappings.Value.MappingFor(entityType);

        public BulkCopyOperation InsertBatch<Model>(SqlBulkCopyOptions options, params Model[] modelList)
        where Model : class => InsertBatch(options, modelList.AsEnumerable());

        public BulkCopyOperation InsertBatch<Model>(SqlBulkCopyOptions options, IEnumerable<Model> modelList)
        where Model : class
        {
            var builder = new DbModelBuilder(DbModelBuilderVersion.Latest);
            var model = builder.Build(new SqlConnection(this.ContextConfig.ConnectionString));
            var efMappings = this.EFMappings.Value;

            var bco = new BulkCopyOperation();
            var converter = new ModelConverter(this);
            var emc = ContextConfig.ModelMapConfigFor<Model>().ThrowIfNull($"Model Map Config not found for: {typeof(Model).FullName}");
            var tentity = emc.EntityType;
            tentity
                .GetProperties()

                //project an object representing properties and their corresponding metadata
                .Select(_pinfo => new
                {
                    Property = _pinfo,
                    EFModel = efMappings.MappingFor(_pinfo.DeclaringType), //<-- In a TPH scenario, different properties may be mapped to different tables
                    PropertyMap = efMappings.MappingFor(_pinfo.DeclaringType).ScalarProperties
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
                    foreach (var _prop in tmapar) //<-- filter out only scalar properties
                        if (_prop.PropertyMap.Key != ScalarPropertyModel.KeyMode.StoreGenerated)
                            _bcxt.ColumnMappings.Add(_prop.PropertyMap.MappedColumn, _prop.PropertyMap.MappedColumn);

                    var columnsAreMapped = false;

                    //populate the datatable
                    foreach (var item in modelList.Select(_model => converter.ToEntity(_model)))
                    {
                        var values = new List<object>();
                        tmapar.Where(_prop => _prop.PropertyMap.Key != ScalarPropertyModel.KeyMode.StoreGenerated).ForAll(_prop =>
                        {
                            var prop = _prop.PropertyMap;
                            if (!columnsAreMapped)
                                bco.PayloadMap[_bcxt].Columns.Add(prop.MappedColumn, Nullable.GetUnderlyingType(prop.ClrProperty.PropertyType) ?? prop.ClrProperty.PropertyType);

                            values.Add(prop.ClrProperty.GetValue(item)); //<-- this should be optimized - proly by caching a delegate to the getter function
                        });

                        columnsAreMapped = true;
                        bco.PayloadMap[_bcxt].Rows.Add(values.ToArray());
                    }
                });

            return bco;
        }


        #region IPersistenceCommands
        
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

        public IOperation<IEnumerable<Model>> AddBatch<Model>(IEnumerable<Model> models)
        where Model : class => LazyOp.Try(() =>
        {
            InsertBatch(SqlBulkCopyOptions.Default, models).Execute();
            return models;
        });
        #endregion

        #region Delete
        public IOperation<Model> Delete<Model>(Model d)
        where Model : class => LazyOp.Try(() =>
        {
            var emc = ContextConfig.ModelMapConfigFor<Model>().ThrowIfNull($"Model Map Config not found for: {typeof(Model).FullName}");
            var set = Set(emc.EntityType);
            var local = GetLocally(set, d);
            var entity = local == null ?
                         set.Attach(d) :
                         local;

            entity = set.Remove(entity);
            SaveChanges();
            
            return new ModelConverter(this).ToModel<Model>(entity);
        });

        public IOperation<IEnumerable<Model>> DeleteBatch<Model>(IEnumerable<Model> models)
        where Model : class => LazyOp.Try(() =>
        {
            var entitiesAreBeignTracked = Configuration.AutoDetectChangesEnabled;
            Configuration.AutoDetectChangesEnabled = false; //<-- to improve performance for batch operations
            try
            {
                var emc = ContextConfig.ModelMapConfigFor<Model>().ThrowIfNull($"Model Map Config not found for: {typeof(Model).FullName}");
                var set = Set(emc.EntityType);
                var deletedEntities = models
                    .Select(_model =>
                    {
                        var local = GetLocally(set, _model);
                        return local == null ?
                               set.Attach(_model) :
                               local;
                    })
                    .Pipe(set.RemoveRange);

                SaveChanges();

                var converter = new ModelConverter(this);
                return deletedEntities
                    .Cast<object>()
                    .Select(_entity => converter.ToModel<Model>(_entity));
            }
            finally
            {
                Configuration.AutoDetectChangesEnabled = entitiesAreBeignTracked;
            }
        });
        #endregion

        #region Update
        public IOperation<Entity> Update<Entity>(Entity entity, Action<Entity> copyFunction = null)
        where Entity : class => UpdateEntity(entity, copyFunction).Then(_e =>
        {
            this.SaveChanges();
            return _e;
        });

        public IOperation<IEnumerable<Entity>> UpdateBatch<Entity>(IEnumerable<Entity> sequence, Action<Entity> copyFunction = null)
        where Entity : class => LazyOp.Try(() =>
        {
            var entitiesAreBeignTracked = this.Configuration.AutoDetectChangesEnabled;
            this.Configuration.AutoDetectChangesEnabled = false; //<-- to improve performance for batch operations
            try
            {
                return sequence
                    .Select(_entity => UpdateEntity(_entity, copyFunction).Resolve())
                    .ToList() //forces evaluation of the IEnumerable
                    .UsingValue(_list => this.SaveChanges());
            }
            finally
            {
                this.Configuration.AutoDetectChangesEnabled = entitiesAreBeignTracked;
            }
        });

        private IOperation<Model> UpdateEntity<Model>(Model model, Action<Model> copyFunction)
        where Model : class => LazyOp.Try(() =>
        {
            var emc = ContextConfig.ModelMapConfigFor<Model>().ThrowIfNull($"Model Map Config not found for: {typeof(Model).FullName}");
            var set = Set(emc.EntityType);
            var local = GetLocally(set, model);

            //if the entity was found locally, apply the copy function or copy from the supplied object
            if (local != null)
            {
                if (copyFunction == null) local.CopyFrom(model);
                else copyFunction.Invoke((Model)local);
            }

            //if the entity wasn't found locally, simply attach it
            else set.Attach(local = model);

            Entry(local).State = EntityState.Modified;

            return new ModelConverter(this).ToModel<Model>(local);
        });
        #endregion

        internal Entity GetLocally<Entity>(DbSet<Entity> set, Entity entity)
        where Entity : class
        {
            //get the object keys. This CAN be cached, but SHOULD it be?
            var keys = this
                .MappingFor<Entity>()
                .ScalarProperties
                .Where(_p => _p.IsKey)
                .Select(_p => _p.ClrProperty.Name.ValuePair(entity.PropertyValue(_p.ClrProperty.Name)));

            //find the entity locally
            return set.Local.FirstOrDefault(_e =>
            {
                return keys.Select(_k => _e.PropertyValue(_k.Key))
                           .SequenceEqual(keys.Select(_k => _k.Value));
            });
        }


        internal object GetLocally(DbSet set, object entity) => GetLocally(set, entity.GetType(), entity);
        internal object GetLocally(DbSet set, Type entityType, object entity)
        => GetLocally(
            set,
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
        #endregion

        #region IQueryCommands

        public IQueryable<Entity> Query<Entity>(params Expression<Func<Entity, object>>[] includes) 
        where Entity : class => includes.Aggregate(Set<Entity>() as IQueryable<Entity>, (_acc, _next) => _acc.Include(_next));

        #endregion

        private static ConcurrentDictionary<string, Action<object, object>> MutatorCache = new ConcurrentDictionary<string, Action<object, object>>();

        private string PropertySignature(PropertyInfo propInfo) => $"[{propInfo.DeclaringType.MinimalAQName()}].{propInfo.Name}";

        private Model TransformEntity<Entity, Model>(Entity entity, Dictionary<object, object> cache)
        where Entity : class 
        where Model : class, new() => Transform(entity, typeof(Model), cache).Cast<Model>();

        private object TransformEntity(object entity, Type modelType, Dictionary<object, object> cache)
        {
            if (cache.ContainsKey(entity)) return cache[entity];
            else
            {
                var _modelMap = ContextConfig.ModelMapConfigFor(modelType);
                var _model = Activator.CreateInstance(modelType);
                cache[entity] = _model;
                var etype = entity.GetType();

                var scalarProps = new HashSet<string>(MappingFor(etype).AllScalarProperties.Select(_sp => _sp.ClrProperty.Name));
                var complexProps = new HashSet<string>(MappingFor(etype).ComplexProperties.Select(_cp => _cp.SourceProperty.Name));
                var navigProps = new HashSet<string>(MappingFor(etype).NavigationProperties.Select(_np => _np.ClrProperty.Name));

                modelType
                    .GetProperties()
                    .ForAll(_mprop =>
                    {
                        var _eprop = etype.GetProperty(_mprop.Name);

                        //property doesnt exist in the entity
                        if (_eprop == null) return;

                        //if this is a simple non-navigation property
                        else if (scalarProps.Contains(_mprop.Name))
                        {
                            if (_eprop.PropertyType != _mprop.PropertyType) throw new Exception($"type mismatch for property {PropertySignature(_mprop)}");

                            object val = null;
                            if (!entity.TryPropertyValue(_mprop.Name, ref val)) return;
                            MutatorCache.GetOrAdd(PropertySignature(_mprop), _msig =>
                            {
                                //create a method that assigns the property: ((Model)model).Property = (PropertyType)val;
                                ParameterExpression pentity = Expression.Parameter(typeof(object)),
                                                    pvalue = Expression.Parameter(typeof(object));
                                var lambda = Expression.Lambda(
                                    Expression.Block(
                                        Expression.Assign(
                                            Expression.MakeMemberAccess(
                                                Expression.Convert(pentity, modelType),
                                                _mprop
                                            ),
                                            Expression.Convert(pvalue, _mprop.PropertyType)
                                        )
                                    ),
                                    pentity, pvalue);

                                return (Action<object, object>)lambda.Compile();
                            })
                            .Invoke(_model, val);
                        }

                        //else if its a complex property...
                        else if (complexProps.Contains(_mprop.Name))
                        {
                            TransformComplexType(_eprop, _mprop);
                        }

                        //else if its a navigation property...
                        else if (navigProps.Contains(_mprop.Name))
                        {
                            //first, assign the scalar part of the navigation property...

                            //now convert the object itself and assign it
                            object val = TransformEntity(entity.PropertyValue(_mprop.Name), _mprop.PropertyType ,cache);
                            MutatorCache.GetOrAdd(PropertySignature(_mprop), _msig =>
                            {
                                //create a method that assigns the property: ((Model)model).Property = (PropertyType)val;
                                ParameterExpression pentity = Expression.Parameter(typeof(object)),
                                                    pvalue = Expression.Parameter(typeof(object));
                                var lambda = Expression.Lambda(
                                    Expression.Block(
                                        Expression.Assign(
                                            Expression.MakeMemberAccess(
                                                Expression.Convert(pentity, modelType),
                                                _mprop
                                            ),
                                            Expression.Convert(pvalue, _mprop.PropertyType)
                                        )
                                    ),
                                    pentity, pvalue);

                                return (Action<object, object>)lambda.Compile();
                            })
                            .Invoke(_model, val);
                        }
                    });

                return _model;
            }
        }

        private void TransformComplexType(PropertyInfo entityProperty, PropertyInfo modelProperty)
        {

        }
    }
}
