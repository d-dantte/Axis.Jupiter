using static Axis.Luna.Extensions.EnumerableExtensions;
using static Axis.Luna.Extensions.ObjectExtensions;

using Axis.Jupiter.Europa.Utils;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;

namespace Axis.Jupiter.Europa
{
    public class EuropaContext : DbContext, IDataContext, IDisposable
    {
        #region Properties
        private ContextConfiguration<EuropaContext> ContextConfig { get; set; }
        private Dictionary<Type, dynamic> _queryGenerators { get; set; } = new Dictionary<Type, dynamic>();
        private Dictionary<string, dynamic> _contextQueries { get; set; } = new Dictionary<string, dynamic>();
        private Dictionary<Type, SqlBulkCopy> _bulkCopyContextMap = new Dictionary<Type, SqlBulkCopy>();

        public EFMappings EFMappings => ContextConfig.EFMappings;
        #endregion

        #region Init

        public EuropaContext(ContextConfiguration<EuropaContext> configuration)
        : base(configuration.ConnectionString, configuration.Compile())
        {
            ContextConfig = configuration;
            Init();
        }

        private void Init()
        {
            //configure EF. Note that only configuration actions should be carried out here.
            ContextConfig.EFContextConfiguration?.Invoke(this.Configuration);

            //load store query generators
            ContextConfig.ConfiguredModules
                .SelectMany(_m => _m.StoreQueryGenerators)
                .ForAll((cnt, next) => _queryGenerators.Add(next.Key, next.Value));

            //load context query generators
            ContextConfig.ConfiguredModules
                .SelectMany(_m => _m.ContextQueryGenerators)
                .ForAll((cnt, next) => _contextQueries.Add(next.Key, next.Value));
            
            //initialize the database if necessary
            new RootDbInitializer<EuropaContext>(ContextConfig.DatabaseInitializer ?? new NullDatabaseInitializer<EuropaContext>(), 
                                                 cxt => cxt.ContextConfig.ConfiguredModules.ForAll((cnt, next) => next.InitializeContext(cxt)))
                .InitializeDatabase(this);
        }

        #endregion

        internal dynamic QueryGeneratorFor<Entity>() => QueryGeneratorFor(typeof(Entity));
        internal dynamic QueryGeneratorFor(Type entitytype) => Eval(() => _queryGenerators[entitytype]);

        #region IDataContext

        public Task BulkInsert<Entity>(IEnumerable<Entity> objectStream)
        where Entity : class => Task.Run(() =>
        {
            typeof(Entity).GetProperties()
                .Select(_propMap => new
                {
                    Property = _propMap,
                    EFModel = ContextConfig.EFMappings.MappingFor(_propMap.DeclaringType),
                    PropertyMap = ContextConfig.EFMappings.MappingFor(_propMap.DeclaringType).Properties.First(_p => PropertiesAreEquivalent(_p.ClrProperty, _propMap))
                })
                .GroupBy(_pmap => _pmap.EFModel.MappedTable)
                .ForAll((_cnt, _tmap) =>
                {
                    var tmapar = _tmap.ToArray();

                    //get or generate the bulk copy context
                    var _bcxt = _bulkCopyContextMap.GetOrAdd(typeof(Entity), _t =>
                    {
                        var bc = new SqlBulkCopy(ContextConfig.ConnectionString, ContextConfig.BulkCopyOptions);
                        bc.DestinationTableName = _tmap.Key;

                        foreach (var _prop in tmapar)
                        {
                            var prop = _prop.PropertyMap;
                            if (prop.Key != PropertyModel.KeyMode.StoreGenerated) bc.ColumnMappings.Add(prop.MappedProperty, prop.MappedProperty);
                        }
                        return bc;
                    });


                    var table = new DataTable();
                    var columnsAreMapped = false;
                    var tasks = new List<Task>();

                    _bcxt.BatchSize = objectStream.Count();
                    tasks.Add(Task.Run(() =>
                    {
                        foreach (var item in objectStream)
                        {
                            var values = new List<object>();
                            tmapar.Where(_prop => _prop.PropertyMap.Key != PropertyModel.KeyMode.StoreGenerated).ForAll((i, _prop) =>
                            {
                                var prop = _prop.PropertyMap;
                                if (!columnsAreMapped)
                                    table.Columns.Add(prop.MappedProperty, Nullable.GetUnderlyingType(prop.ClrProperty.PropertyType) ?? prop.ClrProperty.PropertyType);

                                values.Add(prop.ClrProperty.GetValue(item)); //<-- i should find a way to optimaize this, proly by caching a delegate to the getter function
                            });

                            columnsAreMapped = true;
                            table.Rows.Add(values.ToArray());
                        }

                        _bcxt.WriteToServer(table);
                    }));

                    Task.WaitAll(tasks.ToArray());
                });
        });

        private bool PropertiesAreEquivalent(PropertyInfo first, PropertyInfo second)
            => first.Name == second.Name &&
               first.PropertyType == second.PropertyType &&
               first.DeclaringType == second.DeclaringType;

        public IObjectStore<Entity> Store<Entity>()
        where Entity : class => new ObjectStore<Entity>(this);

        public IQueryable<Entity> ContextQuery<Entity>(string queryIdentity, params object[] args)
        where Entity : class
        {
            var fnc = Eval(() => (_contextQueries[queryIdentity] as Func<IDataContext, object[], IQueryable<Entity>>));
            return fnc?.Invoke(this, args ?? new object[0]) ?? (new Entity[0]).AsQueryable();
        }

        private bool _disposed = false;
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            //dispose internal resources
            if (disposing)
            {
                _bulkCopyContextMap.Values.ForAll((_cnt, _next) => Eval(() => _next.Close()));
            }

            this._disposed = true;
        }

        public int CommitChanges() => this.SaveChanges();

        public Task<int> CommitChangesAsync() => this.SaveChangesAsync();

        public IObjectFactory<Entity> FactoryFor<Entity>()
        where Entity : class => Store<Entity>();


        public bool SupportsBulkPersist => true;
        public string Name => "Axis.Jupitar.Europa";


        #region shortcuts
        public IObjectStore<Entity> Add<Entity>(Entity entity)
        where Entity: class => Store<Entity>().Add(entity);

        public IObjectStore<Entity> Modify<Entity>(Entity entity)
        where Entity: class => Store<Entity>().Modify(entity);

        public IObjectStore<Entity> Delete<Entity>(Entity entity)
        where Entity : class => Store<Entity>().Delete(entity);
        #endregion

        #endregion
    }
}
