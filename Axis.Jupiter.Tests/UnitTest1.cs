using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Axis.Jupiter.Tests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            var alist = new List<A>(new[]
            {
                new A{ Id = 1 },
                new A{ Id = 2 },
                new A{ Id = 3 }
            });
            var blist = new List<B>(new[]
            {
                new B{ Id = 1 , AId = 1},
                new B{ Id = 2 , AId = 2},
                new B{ Id = 3 , AId = 2},
                new B{ Id = 4 , AId = 2},
                new B{ Id = 5 },
                new B{ Id = 6 }
            });
            var clist = new List<C>(new[]
            {
                new C{ Id = 1 , BId = 1},
                new C{ Id = 2 , BId = 2},
                new C{ Id = 4 , BId = 2},
                new C{ Id = 5 , BId = 6},
                new C{ Id = 6 , BId = 3},
                new C{ Id = 7 }
            });
            var dlist = new List<D>(new[]
            {
                new D{ Id = 1 , CId = 1},
                new D{ Id = 2 , CId = 2},
                new D{ Id = 4 , CId = 2},
                new D{ Id = 5 , CId = 6},
                new D{ Id = 6 , CId = 3},
                new D{ Id = 7 }
            });

            var g = from a in alist
                    join b in blist on a.Id equals b.AId into barr
                    from bb in barr.DefaultIfEmpty()
                    join c in clist on bb?.Id equals c.BId into carr
                    from cc in carr.DefaultIfEmpty()
                    join d in dlist on cc?.Id equals d.CId
                    select new { a, bb, cc, d };
            var l = g.ToArray();

            Console.WriteLine(g);
        }
    }

    public abstract class Base
    {
        public int Id { get; set; }

        public override string ToString() => $"{GetType().Name}::{Id}";
    }

    public class A: Base
    {
        public List<B> BList { get; set; } = new List<B>();
    }

    public class B: Base
    {
        public int AId { get; set; }
        public List<C> CList { get; set; } = new List<C>();
    }

    public class C: Base
    {
        public int BId { get; set; }
        public List<D> DList { get; set; } = new List<D>();
    }

    public class D: Base
    {
        public int CId { get; set; }
    }
}
