namespace Axis.Jupiter.MongoDb.XModels
{
    public interface IMongoEntity
    {
        object Key { get; set; }

        int GetHashCode();

        bool Equals(object other);
    }


    public interface IMongoEntity<TKey>: IMongoEntity
    {
        new TKey Key { get; set; }
    }
}
