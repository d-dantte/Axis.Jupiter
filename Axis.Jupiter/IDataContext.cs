using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Axis.Jupiter
{
    public interface IDataContext: IObjectFactoryGenerator, IDisposable
    {
        IQueryable<Entity> ContextQuery<Entity>(string queryIdentity) where Entity : class;

        IObjectStore<Entity> Store<Entity>() where Entity : class;

        int CommitChanges();
        Task<int> CommitChangesAsync();

        Task BulkInsert<Entity>(IEnumerable<Entity> objectStream) where Entity : class;
        bool SupportsBulkPersist { get; }

        string Name { get; }
    }
}
