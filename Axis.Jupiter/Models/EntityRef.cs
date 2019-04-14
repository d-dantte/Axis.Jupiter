
using System;

namespace Axis.Jupiter.Models
{

    public class EntityRef: IEntityReference
    {
        public RefType RefType { get; set; }
        public string Name { get; set; }
        public EntityGraph Ref { get; set; }

        /// <summary>
        /// A delegate used for Secondary RefTypes.
        /// When executed, it should set the Relevant Property of the <c>Ref.Entity</c> object to the Id of the 
        /// object that owns this Ref instance.
        /// </summary>
        public Action BindId { get; set; }
    }
}
