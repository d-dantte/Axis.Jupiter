using Axis.Luna.Operation;
using System;
using System.Collections.Generic;

namespace Axis.Jupiter.Kore.Commands
{
    public interface IPersistenceCommands: IDisposable
    {
        IOperation<Model> Add<Model>(Model d) where Model : class;
        IOperation<IEnumerable<Model>> AddBatch<Model>(IEnumerable<Model> d) where Model : class;

        IOperation<Model> Update<Model>(Model d, Action<Model> copyFunction = null) where Model : class;
        IOperation<IEnumerable<Model>> UpdateBatch<Model>(IEnumerable<Model> d, Action<Model> copyFunction = null) where Model : class;

        IOperation<Model> Delete<Model>(Model d) where Model : class;
        IOperation<IEnumerable<Model>> DeleteBatch<Model>(IEnumerable<Model> d) where Model : class;
    }
}
