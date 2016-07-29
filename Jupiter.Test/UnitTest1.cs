using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
    }
}
