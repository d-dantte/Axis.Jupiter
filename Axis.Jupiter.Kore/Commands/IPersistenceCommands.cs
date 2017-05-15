using Axis.Luna;
using System.Collections.Generic;

namespace Axis.Jupiter.Kore.Commands
{
    public interface IPersistenceCommands
    {
        IOperation<Domain> Add<Domain>(Domain d) where Domain : class;
        IOperation<IEnumerable<Domain>> AddBatch<Domain>(IEnumerable<Domain> d) where Domain : class;

        IOperation<Domain> Update<Domain>(Domain d) where Domain : class;
        IOperation<IEnumerable<Domain>> UpdateBatch<Domain>(IEnumerable<Domain> d) where Domain : class;

        IOperation<Domain> Delete<Domain>(Domain d) where Domain : class;
        IOperation<Domain> DeleteBatch<Domain>(IEnumerable<Domain> d) where Domain : class;
    }
}
