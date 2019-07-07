using System;
using System.Collections.Generic;

namespace Axis.Jupiter.MongoDb.ConsoleTest.Models
{
    public class User: BaseModel<Guid>
    {
        public BioData Bio { get; set; }
        public int Status { get; set; }
        public string Name { get; set; }

        public List<Role> Roles { get; } = new List<Role>();
    }
}
