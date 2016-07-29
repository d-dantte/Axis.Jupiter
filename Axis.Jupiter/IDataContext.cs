using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Axis.Jupiter
{
    public interface IDataContext: IObjectFactoryGenerator, IDisposable
    {
        IObjectStore<Object> Store<Object>() where Object : class;
        int CommitChanges();
        Task<int> CommitChangesAsync();

        Task BulkInsert<Object>(IEnumerable<Object> objectStream) where Object : class;
        bool SupportsBulkPersist { get; }

        string Name { get; }
    }
}
