using System;
using System.Collections.Generic;
using System.Text;

namespace Axis.Jupiter.EFCore.ConsoleTest.Models
{
    public class Permission: BaseModel<Guid>
    {
        public string Name { get; set; }
        public string Scope { get; set; }

        public Role Role { get; set; }
    }
}
