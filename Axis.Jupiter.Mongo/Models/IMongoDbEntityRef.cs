namespace Axis.Jupiter.MongoDb.Models
{
    internal interface IMongoDbEntityRef
    {
        string DbCollection { get; set; }

        string DbLabel { get; set; }
    }
}
