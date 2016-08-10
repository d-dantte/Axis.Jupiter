using static Axis.Luna.Extensions.ExceptionExtensions;
using static Axis.Luna.Extensions.EnumerableExtensions;
using static Axis.Luna.Extensions.ObjectExtensions;

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
        internal EFMapping ContextMetadata { get; set; }
        private ContextConfiguration ContextConfig { get; set; }
        private Dictionary<Type, dynamic> _queryGenerators { get; set; } = new Dictionary<Type, dynamic>();


        protected EuropaContext()
        {
            Init();
        }

        protected EuropaContext(string cstring): base(cstring)
        {
            Init();
        }

        public EuropaContext(ContextConfiguration configuration):base(configuration.ConnectionString)
        {
            ContextConfig = configuration.ThrowIfNull();
            Init();
        }

        private void Init()
        {
            _bulkContext = new SqlBulkCopy(Database.Connection.ConnectionString);

            //load query generators
            ContextConfig.Modules.Values
                .SelectMany(_m => _m.QueryGenerators)
                .ForAll((cnt, next) => _queryGenerators.Add(next.Key, next.Value));
        }

        internal dynamic QueryGeneratorFor<Entity>() => QueryGeneratorFor(typeof(Entity));
        internal dynamic QueryGeneratorFor(Type entitytype) => Eval(() => _queryGenerators[entitytype]);


        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            //set db initializer - and any seeding logic
            Action<EuropaContext> grandSeeder = cxt => ContextConfig?.Modules.Values.ForAll((cnt, next) => next.SeedContext(this));
            Database.SetInitializer(new RootDbInitializer<EuropaContext>(ContextConfig?.DatabaseInitializer, grandSeeder));

            //provide entity configurations
            ContextConfig?.Modules.Values.ForAll((cnt, next) => next.ConfigureContext(modelBuilder));     
        }


        #region IDataContext

        private SqlBulkCopy _bulkContext = null;
        public Task BulkInsert<Entity>(IEnumerable<Entity> objectStream) where Entity : class
        {
            ContextMetadata = ContextMetadata ?? new EFMapping(this);

            return Task.Run(() =>
            {
                var tableName = ContextMetadata.typeMetadata<Entity>().table.TableName;
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
