using System;
using System.Collections.Generic;

namespace Axis.Jupiter.Models
{

    public class ListRef: IEntityReference
    {
        public ListType ListType { get; set; }
        public string Name { get; set; }
        public List<EntityGraph> Entities { get; } = new List<EntityGraph>();

        /// <summary>
        /// When executed, it should set the Relevant Property of all the objects in the <c>Ref.Entities</c> list to the Id of the 
        /// object that owns this Ref instance.
        /// </summary>
        public Action BindId { get; set; }
    }
}
