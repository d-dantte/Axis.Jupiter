using Axis.Luna;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static Axis.Luna.Extensions.ExceptionExtensions;

namespace Axis.Jupiter.Kore.Command
{
    public class DefaultPersistenceCommand : IPersistenceCommands
    {
        private IDataContext _context = null;
        private PersistenceProvider _persistenceProvider = null;
        private DomainConverter _converter = null;

        public DefaultPersistenceCommand(IDataContext context, PersistenceProvider persistenceProvider, DomainConverter converter)
        {
            ThrowNullArguments(() => context,
                               () => converter,
                               () => persistenceProvider);

            _context = context;
            _converter = converter;
            _persistenceProvider = persistenceProvider;
        }

        public Operation<Domain> Delete<Domain>(Domain d)
        where Domain : class => Operation.Try(() =>
        {
            if (_persistenceProvider.CanDelete<Domain>()) return _persistenceProvider.Delete(d, _converter);

            _context.Store<Domain>().Delete(d, true);
            return d;
        });

        public Operation<Domain> Add<Domain>(Domain d)
        where Domain : class => Operation.Try(() =>
        {
            if (_persistenceProvider.CanInsert<Domain>()) return _persistenceProvider.Insert(d, _converter);

            _context.Add(d).Context.CommitChanges();
            return d;
        });

        public Operation<Domain> Update<Domain>(Domain d)
        where Domain : class => Operation.Try(() =>
        {
            if (_persistenceProvider.CanUpdate<Domain>()) return _persistenceProvider.Update(d, _converter);

            _context.Store<Domain>().Modify(d, true);
            return d;
        });

        public AsyncOperation<IEnumerable<Domain>> AddBulk<Domain>(IEnumerable<Domain> d)
        where Domain : class => Operation.TryAsync(() =>
        {
            if (_persistenceProvider.CanBulkInsert<Domain>()) return _persistenceProvider.BulkInsert(d, _converter);

            _context.BulkInsert(d).Wait();
            return d;
        });
    }
}
