using Axis.Luna;
using Axis.Luna.Extensions;
using System;
using System.Collections.Generic;

namespace Axis.Jupiter.Kore
{
    public class PersistenceProvider
    {
        private IDataContext _context = null;

        public Registrar OperationCache { get; } = new Registrar();

        public PersistenceProvider(IDataContext context, Action<Registrar> operationRegistration  = null)
        {
            operationRegistration?.Invoke(OperationCache);

            _context = context.ThrowIfNull("invalid context supplied");
        }

        public bool CanBulkInsert<Model>() => OperationCache.BulkInsertOperations.ContainsKey(typeof(Model));
        public bool CanInsert<Model>() => OperationCache.InsertOperations.ContainsKey(typeof(Model));
        public bool CanUpdate<Model>() => OperationCache.UpdateOperations.ContainsKey(typeof(Model));
        public bool CanDelete<Model>() => OperationCache.DeleteOperations.ContainsKey(typeof(Model));


        public IEnumerable<Model> BulkInsert<Model>(IEnumerable<Model> darr, DomainConverter converter)
        => ((Func<IEnumerable<Model>, IDataContext, DomainConverter, IEnumerable<Model>>)OperationCache.BulkInsertOperations[typeof(Model)]).Invoke(darr, _context, converter);

        public Model Insert<Model>(Model d, DomainConverter converter)
        => ((Func<Model, IDataContext, DomainConverter, Model>)OperationCache.InsertOperations[typeof(Model)]).Invoke(d, _context, converter);

        public Model Update<Model>(Model d, DomainConverter converter)
        => ((Func<Model, IDataContext, DomainConverter, Model>)OperationCache.UpdateOperations[typeof(Model)]).Invoke(d, _context, converter);

        public Model Delete<Model>(Model d, DomainConverter converter)
        => ((Func<Model, IDataContext, DomainConverter, Model>)OperationCache.DeleteOperations[typeof(Model)]).Invoke(d, _context, converter);


        
        public class Registrar
        {
            internal Dictionary<Type, dynamic> BulkInsertOperations = new Dictionary<Type, dynamic>();
            internal Dictionary<Type, dynamic> InsertOperations = new Dictionary<Type, dynamic>();
            internal Dictionary<Type, dynamic> UpdateOperations = new Dictionary<Type, dynamic>();
            internal Dictionary<Type, dynamic> DeleteOperations = new Dictionary<Type, dynamic>();

            public Registrar RegisterBulkInsert<Model>(Func<IEnumerable<Model>, IDataContext, DomainConverter, IEnumerable<Model>> inserter)
            {
                InsertOperations[typeof(Model)] = inserter.ThrowIfNull();
                return this;
            }
            public Registrar RegisterInsert<Model>(Func<Model, IDataContext, DomainConverter, Model> inserter)
            {
                InsertOperations[typeof(Model)] = inserter.ThrowIfNull();
                return this;
            }
            public Registrar RegisterUpdate<Model>(Func<Model, IDataContext, DomainConverter, Model> updater)
            {
                UpdateOperations[typeof(Model)] = updater.ThrowIfNull();
                return this;
            }
            public Registrar RegisterDelete<Model>(Func<Model, IDataContext, DomainConverter, Model> deleter)
            {
                DeleteOperations[typeof(Model)] = deleter.ThrowIfNull();
                return this;
            }
        }
    }
}
