using System;
using System.Collections.Generic;
using System.Text;

namespace Axis.Jupiter.Models
{
    public interface IEntityReference
    {
        string Name { get; }
        Action BindId { get; }
    }
}
