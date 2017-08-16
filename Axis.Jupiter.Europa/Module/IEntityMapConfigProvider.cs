using Axis.Jupiter.Europa.Mappings;
using System.Collections.Generic;

namespace Axis.Jupiter.Europa.Module
{
    interface IEntityMapConfigProvider
    {
        IEnumerable<IEntityMapConfiguration> ConfiguredEntityMaps();
    }
}
