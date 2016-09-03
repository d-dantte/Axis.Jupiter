using System.Data.Entity.ModelConfiguration;

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
