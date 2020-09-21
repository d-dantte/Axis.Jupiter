using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using System;

namespace Axis.Jupiter.MongoDb.ConsoleTest.Entities
{
    public class BioData
    {
        [JsonIgnore, BsonIgnore]
        public User Owner { get; set; }

        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }

        public Models.Sex Sex { get; set; }

        public DateTimeOffset DateOfBirth { get; set; }
        public string Nationality { get; set; }
    }
}
