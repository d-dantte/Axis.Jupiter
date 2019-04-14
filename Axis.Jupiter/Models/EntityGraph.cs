using System;
using System.Collections.Generic;
using System.Text;

namespace Axis.Jupiter.Models
{
    public class EntityGraph
    {
        public object Entity { get; set; }
        public List<EntityRef> EntityRefs { get; } = new List<EntityRef>();
        public List<ListRef> ListRefs { get; } = new List<ListRef>();
    }
}
