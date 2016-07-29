using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Axis.Jupiter
{
    public interface IObjectFactoryGenerator
    {
        IObjectFactory<DomainObject> FactoryFor<DomainObject>() where DomainObject : class;
    }
}
