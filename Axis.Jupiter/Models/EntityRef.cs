
namespace Axis.Jupiter.Models
{

    public class EntityRef
    {
        public EntityRefType RefType { get; set; }
        public EntityRefRelativity RefRelativity { get; set; }

        public string Name { get; set; }
        public object Entity { get; set; }
    }
}
