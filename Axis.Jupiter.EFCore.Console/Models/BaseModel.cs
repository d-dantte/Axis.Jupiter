using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Axis.Jupiter.EFCore.ConsoleTest.Models
{
    public abstract class BaseModel<Key>
    {
        public Key Id { get; set; }

        public DateTimeOffset CreatedOn { get; set; }
        public Guid CreatedBy { get; set; }

        public bool IsPersisted { get; set; }
    }
}
