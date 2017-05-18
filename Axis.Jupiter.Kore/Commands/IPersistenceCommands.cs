using Axis.Luna.Operation;
using System;
using System.Collections.Generic;

namespace Axis.Jupiter.Kore.Commands
{
    public interface IPersistenceCommands: IDisposable
    {
        IOperation<Entity> Add<Entity>(Entity d) where Entity : class;
        IOperation<IEnumerable<Entity>> AddBatch<Entity>(IEnumerable<Entity> d) where Entity : class;

        IOperation<Entity> Update<Entity>(Entity d, Action<Entity> copyFunction = null) where Entity : class;
        IOperation<IEnumerable<Entity>> UpdateBatch<Entity>(IEnumerable<Entity> d, Action<Entity> copyFunction = null) where Entity : class;

        IOperation<Entity> Delete<Entity>(Entity d) where Entity : class;
        IOperation<IEnumerable<Entity>> DeleteBatch<Entity>(IEnumerable<Entity> d) where Entity : class;
    }
}
