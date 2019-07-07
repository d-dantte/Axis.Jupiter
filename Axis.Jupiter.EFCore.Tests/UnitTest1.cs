using Axis.Jupiter.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

using static Axis.Jupiter.Helpers.Paths;

namespace Axis.Jupiter.EFCore.Tests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            var path = From<A>();

            var dots = Flatten(path);
            Console.WriteLine(dots);

            var path2 = path
                .To(a => a.B)
                .To(b => b.As)
                .To(a => a.B);

            dots = Flatten(path2);
            Console.WriteLine(dots);
        }




        public static string Flatten(IPropertyPath path)
        {
            if (path.IsOrigin)
                return "";

            else if (path.Parent.IsOrigin)
                return path.Property;

            else
                return $"{Flatten(path.Parent)}.{path.Property}";
        }
    }

    public class A
    {
        public B B { get; }
    }

    public class B
    {
        public List<A> As { get; }
    }
}
