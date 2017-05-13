using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Core.Domain
{
    public class Contact
    {
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public long Id { get; set; }

        public WebSite Web { get; set; }

        public virtual Person Owner { get; set; }
        public long OwnerId { get; set; }


    }
    public class WebSite
    {
        public string Host { get; set; }
        public string Page { get; set; }
    }
}
