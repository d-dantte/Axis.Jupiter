using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Core.Domain
{
    public abstract class Course
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class OnsiteCourse: Course
    {
        public string Venue { get; set; }
    }

    public class OnlineCourse:Course
    {
        public string Url { get; set; }
    }
}
