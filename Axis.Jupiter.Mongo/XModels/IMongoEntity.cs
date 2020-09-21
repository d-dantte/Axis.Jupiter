using MongoDB.Bson;

namespace Axis.Jupiter.MongoDb.XModels
{
    public interface IMongoEntity
    {
        ObjectId _id { get; set; }

        object Key { get; set; }

        bool IsPersisted { get; }

        int GetHashCode();

        bool Equals(object other);
    }


    public interface IMongoEntity<TKey>: IMongoEntity
    {
        new TKey Key { get; set; }
    }
}
