using System;
using System.Collections.Generic;
using System.Text;

namespace Axis.Jupiter.EFCore.ConsoleTest.Entities
{
    public abstract class BaseEntity<Key>
    {
        public virtual Key Id { get; set; }

        public DateTimeOffset CreatedOn { get; set; }
        public Guid CreatedBy { get; set; }
    }
}
