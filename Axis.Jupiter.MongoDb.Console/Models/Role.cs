using System;
using System.Collections.Generic;

namespace Axis.Jupiter.MongoDb.ConsoleTest.Models
{
    public class Role: BaseModel<Guid>
    {
        public string Name { get; set; }

        public List<Permission> Permissions { get; } = new List<Permission>();

        public List<User> Users { get; } = new List<User>();
    }
}
