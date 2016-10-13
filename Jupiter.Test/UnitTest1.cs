using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data.Entity;
using Axis.Jupiter.Europa.LinqProvider;
using System.Linq.Expressions;
using System.Linq;

using Axis.Luna.Extensions;

namespace Jupiter.Test
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            dynamic v = 21;
            Action<int> action = r => Console.WriteLine(r);
            action.Invoke(554);
            action.Invoke(v);

            dynamic dynAction = action;
            dynAction.Invoke(60);
            dynAction.Invoke(v);
        }

        [TestMethod]
        public void TestMethod2()
        {
            var q = new Queryable<SomeClass>().As<IQueryable<SomeClass>>();
            q = q.Where(k => true);
            q.ToArray();

            var classes = new SomeClass().Enumerate().AsQueryable();
        }
    }

    public class SomeClass
    {
        public string ABCD { get; set; }
        public int XYZ { get; set; }
    }

    public class Context: DbContext
    {
        public Context()
        {
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            Database.SetInitializer(new DropCreateDatabaseAlways<Context>());

            base.OnModelCreating(modelBuilder);
        }
    }
}
