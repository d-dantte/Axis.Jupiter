
namespace Axis.Jupiter.Models
{
    public enum EntityRefType
    {
        OneToOne,
        OneToMany,
        ManyToMany
    }

    public enum EntityRefRelativity
    {
        Source,
        Destination
    }

    public enum TransformCommand
    {
        Add,
        Update,
        Remove,
        Query
    }
}
