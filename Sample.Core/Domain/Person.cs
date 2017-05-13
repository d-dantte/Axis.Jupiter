using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Core.Domain
{
    public class Person
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime DateOfBirth { get; set; }
        public long Id { get; set; }

        public virtual ICollection<Contact> ContactInfo { get; set; } = new HashSet<Contact>();
    }
}
