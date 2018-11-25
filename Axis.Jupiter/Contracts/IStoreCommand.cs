using System.Collections.Generic;
using Axis.Luna.Operation;

namespace Axis.Jupiter.Contracts
{
    public interface IStoreCommand
    {
        string StoreName { get; }

        Operation<Model> Add<Model>(Model d) where Model : class;
        Operation AddBatch<Model>(IEnumerable<Model> d) where Model : class;

        Operation<Model> Update<Model>(Model d) where Model : class;
        Operation UpdateBatch<Model>(IEnumerable<Model> d) where Model : class;

        Operation<Model> Delete<Model>(Model d) where Model : class;
        Operation DeleteBatch<Model>(IEnumerable<Model> d) where Model : class;
    }
}
