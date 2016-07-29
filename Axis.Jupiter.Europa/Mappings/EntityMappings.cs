using System;
using System.Collections.Generic;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Axis.Jupiter.Europa.Mappings
{
    public abstract class BaseMap<EType> : EntityTypeConfiguration<EType>
    where EType : class
    {
        protected BaseMap(bool useDefaultTable)
        {
            if (useDefaultTable) this.MapToDefaultTable();
        }
        protected BaseMap() :
        this(true)
        { }
    }

    public abstract class BaseComplexMap<Complex> : ComplexTypeConfiguration<Complex>
    where Complex : class
    {
        protected BaseComplexMap()
        {
            //not sure what comes here yet.
        }
    }
}
