using Axis.Luna.Operation;
using System;
using System.Collections.Generic;

namespace Axis.Jupiter.Commands
{
    public interface IPersistenceCommands: IDisposable
    {
        IOperation<Model> Add<Model>(Model d) where Model : class;
        IOperation AddBatch<Model>(IEnumerable<Model> d, int batchSize = 0) where Model : class;

        IOperation<Model> Update<Model>(Model d, Action<Model> copyFunction = null) where Model : class;
        IOperation UpdateBatch<Model>(IEnumerable<Model> d, int batchSize = 0) where Model : class;

        IOperation<Model> Delete<Model>(Model d) where Model : class;
        IOperation DeleteBatch<Model>(IEnumerable<Model> d, int batchSize = 0) where Model : class;
    }
}
