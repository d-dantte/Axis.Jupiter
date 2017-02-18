using Axis.Luna;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Axis.Jupiter.Kore.Command
{
    public interface IPersistenceCommands
    {
        Operation<Domain> Add<Domain>(Domain d) where Domain : class;
        Operation<IEnumerable<Domain>> AddBulk<Domain>(IEnumerable<Domain> d) where Domain : class;
        Operation<Domain> Update<Domain>(Domain d) where Domain : class;
        Operation<Domain> Delete<Domain>(Domain d) where Domain : class;
    }
}
