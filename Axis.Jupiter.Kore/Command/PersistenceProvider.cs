using Axis.Luna.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Axis.Jupiter.Kore
{
    public class PersistenceProvider
    {
        private Registrar _registrar = new Registrar();
        private IDataContext _context = null;

        public PersistenceProvider(IDataContext context, Action<Registrar> operationRegistration)
        {
            operationRegistration.ThrowIfNull("invalid registrations")
                                 .Invoke(_registrar);

            _context = context.ThrowIfNull("invalid context supplied");
        }

        public bool CanInsert<Domain>() => _registrar.InsertOperations.ContainsKey(typeof(Domain));
        public bool CanUpdate<Domain>() => _registrar.UpdateOperations.ContainsKey(typeof(Domain));
        public bool CanDelete<Domain>() => _registrar.DeleteOperations.ContainsKey(typeof(Domain));


        public Domain Insert<Domain>(Domain d)
        => ((Func<Domain, IDataContext, Domain>)_registrar.InsertOperations[typeof(Domain)]).Invoke(d, _context);

        public Domain Update<Domain>(Domain d)
        => ((Func<Domain, IDataContext, Domain>)_registrar.UpdateOperations[typeof(Domain)]).Invoke(d, _context);

        public Domain Delete<Domain>(Domain d)
        => ((Func<Domain, IDataContext, Domain>)_registrar.DeleteOperations[typeof(Domain)]).Invoke(d, _context);


        
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
