
namespace Axis.Jupiter.MongoDb.XModels
{
    public interface IEntityRef: IRefIdentity, IRefDbInfo, IRefInstance
    {
    }


    public interface IEntityRef<out TKey>: IEntityRef, IRefIdentity<TKey>
    {
    }
}
