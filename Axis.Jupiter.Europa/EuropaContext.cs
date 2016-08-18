using static Axis.Luna.Extensions.ExceptionExtensions;
using static Axis.Luna.Extensions.EnumerableExtensions;
using static Axis.Luna.Extensions.ObjectExtensions;
using static Axis.Luna.Extensions.TypeExtensions;

using Axis.Jupiter.Europa.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Axis.Jupiter.Europa;

namespace Axis.Jupiter.Europa
{
    public class EuropaContext: DbContext, IDataContext, IDisposable
    {
        public EFMapping ContextMetadata { get; internal set; }
        private ContextConfiguration ContextConfig { get; set; }
        private Dictionary<Type, dynamic> _queryGenerators { get; set; } = new Dictionary<Type, dynamic>();
        private Dictionary<string, dynamic> _contextQueries { get; set; } = new Dictionary<string, dynamic>();


        #region Init
        private static void CustomInitialiation(EuropaContext cxt)
        {
            //at this point, the Context-Model has been built, so...
            cxt.ContextMetadata = new EFMapping(cxt);

            cxt.ContextConfig?.Modules.Values.ForAll((cnt, next) => next.InitializeContext(cxt));
        }

        protected EuropaContext()
        {
            Init();
        }

        protected EuropaContext(string cstring): base(cstring)
        {
            Init();
        }


        public EuropaContext(ContextConfiguration configuration)
        : base(configuration.UsingValue(_c => Database.SetInitializer(new RootDbInitializer<EuropaContext>(_c.DatabaseInitializer, (Action<EuropaContext>)CustomInitialiation))).ConnectionString)
        {
            ContextConfig = configuration.ThrowIfNull();
            Init();
        }

        private void Init()
        {
            _bulkContext = new SqlBulkCopy(Database.Connection.ConnectionString, ContextConfig.BulkCopyOptions);

            //configure EF. Note that only configuration actions should be carried out here.
            ContextConfig.EFContextConfiguration?.Invoke(this.Configuration);

            //load store query generators
            ContextConfig.Modules.Values
                .SelectMany(_m => _m.StoreQueryGenerators)
                .ForAll((cnt, next) => _queryGenerators.Add(next.Key, next.Value));

            //load context query generators
            ContextConfig.Modules.Values
                .SelectMany(_m => _m.ContextQueryGenerators)
                .ForAll((cnt, next) => _contextQueries.Add(next.Key, next.Value));
        }

        #endregion

        internal dynamic QueryGeneratorFor<Entity>() => QueryGeneratorFor(typeof(Entity));
        internal dynamic QueryGeneratorFor(Type entitytype) => Eval(() => _queryGenerators[entitytype]);


        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //provide entity configurations
            ContextConfig?.Modules.Values.ForAll((cnt, next) => next.ConfigureContext(modelBuilder));     
        }


        #region IDataContext

        private SqlBulkCopy _bulkContext = null;
        public Task BulkInsert<Entity>(IEnumerable<Entity> objectStream) where Entity : class
        {
            //ensure the contextmetadata has been set
            if(ContextMetadata == null)
            {
                //equivalent to "this.Store<Entity>().Query.FirstOrDefault();"
                //get any of the entities
                var anyEntity = ContextConfig.ConfiguredEntityTypes().First();
                var _storeMethod = typeof(IDataContext).GetMethod(nameof(IDataContext.Store)).MakeGenericMethod(anyEntity);
                var _storeObject = _storeMethod.Invoke(this, new object[0]);

                _storeObject.GetType()
                    .GetMethod(nameof(IObjectFactory<object>.NewObject))
                    .Invoke(_storeObject, new object[0]);

                //typeof(Enumerable)
                //    .GetMethods()
                //    .Where(_m => _m.Name == nameof(Enumerable.FirstOrDefault))
                //    .Where(_m => _m.GetParameters().Length == 1)
                //    .First()
                //    .MakeGenericMethod(anyEntity)
                //    .Invoke(null, new object[] { _queryObject });

                //Database.Initialize(false); //<-- this doesnt use my custom initializer
            }

            return Task.Run(() =>
            {
                var tableName = ContextMetadata.TypeMetadata<Entity>().Table.TableName;
                //var tableName = this.TypeMetadata<Entity>().TableName;
                _bulkContext.BatchSize = objectStream.Count();
                _bulkContext.DestinationTableName = tableName;

                var table = new DataTable();
                var props = TypeDescriptor.GetProperties(typeof(Entity))
                    //Dirty hack to make sure we only have system data types 
                    //i.e. filter out the relationships/collections
                                          .Cast<PropertyDescriptor>()
                                          .Where(propertyInfo => propertyInfo.PropertyType.Namespace.Equals("System"))
                                          .ToArray();

                foreach (var propertyInfo in props)
                {
                    _bulkContext.ColumnMappings.Add(propertyInfo.Name, propertyInfo.Name);
                    table.Columns.Add(propertyInfo.Name, Nullable.GetUnderlyingType(propertyInfo.PropertyType) ?? propertyInfo.PropertyType);
                }

                var values = new object[props.Length];
                foreach (var item in objectStream)
                {
                    for (var i = 0; i < values.Length; i++) values[i] = props[i].GetValue(item);

                    table.Rows.Add(values);
                }

                _bulkContext.WriteToServer(table);
            });
        }

        public IObjectStore<Entity> Store<Entity>()
        where Entity : class => new ObjectStore<Entity>(this);

        public IQueryable<Entity> ContextQuery<Entity>(string queryIdentity, params object[] args)
        where Entity : class
        {
            var fnc = Eval(() => (_contextQueries[queryIdentity] as Func<IDataContext, object[], IQueryable<Entity>>));
            return fnc?.Invoke(this, args ?? new object[0]) ?? (new Entity[0]).AsQueryable();
        }

        protected override void Dispose(bool disposing)
        {
            //dispose internal resources
            if (disposing)
            {
                try
                {
                    this._bulkContext.Close();
                }
                catch
                { }
            }
        }

        public int CommitChanges() => this.SaveChanges();

        public Task<int> CommitChangesAsync() => this.SaveChangesAsync();

        public IObjectFactory<Entity> FactoryFor<Entity>()
        where Entity: class => Store<Entity>();
        

        public bool SupportsBulkPersist => true;
        public string Name => "Axis.Jupitar.Europa";

        #endregion
    }
}
