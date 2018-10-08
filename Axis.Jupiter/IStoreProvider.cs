namespace Axis.Jupiter
{
    public interface IStoreProvider
    {
        IStoreCommand CommandFor(string storeId);
        IStoreQuery QueryFor(string storeId);

        IStoreCommand DefaultStoreCommand();
        IStoreQuery DefaultStorQuery();
    }
}