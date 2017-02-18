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

        public bool CanInsert<Domain>() => OperationCache.InsertOperations.ContainsKey(typeof(Domain));
        public bool CanUpdate<Domain>() => OperationCache.UpdateOperations.ContainsKey(typeof(Domain));
        public bool CanDelete<Domain>() => OperationCache.DeleteOperations.ContainsKey(typeof(Domain));


        public Domain Insert<Domain>(Domain d)
        => ((Func<Domain, IDataContext, Domain>)OperationCache.InsertOperations[typeof(Domain)]).Invoke(d, _context);

        public Domain Update<Domain>(Domain d)
        => ((Func<Domain, IDataContext, Domain>)OperationCache.UpdateOperations[typeof(Domain)]).Invoke(d, _context);

        public Domain Delete<Domain>(Domain d)
        => ((Func<Domain, IDataContext, Domain>)OperationCache.DeleteOperations[typeof(Domain)]).Invoke(d, _context);


        
        public class Registrar
        {
            internal Dictionary<Type, dynamic> InsertOperations = new Dictionary<Type, dynamic>();
            internal Dictionary<Type, dynamic> UpdateOperations = new Dictionary<Type, dynamic>();
            internal Dictionary<Type, dynamic> DeleteOperations = new Dictionary<Type, dynamic>();

            public Registrar RegisterInsert<Domain>(Func<Domain, IDataContext, Domain> inserter)
            {
                InsertOperations[typeof(Domain)] = inserter.ThrowIfNull();
                return this;
            }
            public Registrar RegisterUpdate<Domain>(Func<Domain, IDataContext, Domain> updater)
            {
                UpdateOperations[typeof(Domain)] = updater.ThrowIfNull();
                return this;
            }
            public Registrar RegisterDelete<Domain>(Func<Domain, IDataContext, Domain> deleter)
            {
                DeleteOperations[typeof(Domain)] = deleter.ThrowIfNull();
                return this;
            }
        }
    }
}
