using Axis.Jupiter.Services;
using System.Collections.Generic;

namespace Axis.Jupiter.Helpers
{

    public class MappingContext
    {
        internal Dictionary<object, object> Mappers { get; } = new Dictionary<object, object>();

        public EntityMapper EntityMapper { get; }

        internal MappingContext(EntityMapper mapper)
        {
            EntityMapper = mapper;
        }
    }
}
