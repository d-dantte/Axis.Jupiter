using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Core.Domain
{
    public class Mammal
    {
        public float Weight { get; set; }

        public long Id { get; set; }
    }

    public class Cat: Mammal
    {
        public string Feline { get; set; }
    }
    public class Dog: Mammal
    {
        public string Kanine { get; set; }
    }
}
