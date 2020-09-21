using System;
using System.Collections.Generic;
using System.Text;

namespace Axis.Jupiter.MongoDb.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class MongoIndex: Attribute
    {
    }
}
