using Axis.Jupiter.MongoDb.Converters;
using Axis.Jupiter.MongoDb.XModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Axis.Jupiter.Mongo.Tests
{
    [TestClass]
    public class SerializationTests
    {
        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            Converters = new List<JsonConverter>
            {
                new SetRefEntityJsonConverter(),
                new EntityRefJsonConverter(),
                new EntityRefJsonConverter()
            }
        };

        [TestMethod]
        public void TestMethod1()
        {
            var entity = new First
            {
                Key = Guid.NewGuid(),
                Name = "Shit",
                Something = 54,
                Second = new EntityRef<Second, Guid>(
                    collection: "Bleh",
                    dblabel: "BlehDb",
                    entity: new Second
                    {
                        Key = Guid.NewGuid(),
                        TimeStamp = DateTimeOffset.Now
                    })
            };

            var json = JsonConvert.SerializeObject(entity, Settings);
            var reconstructed = JsonConvert.DeserializeObject<First>(json, Settings);

            //perfs
            var count = 10000;
            var start = DateTime.Now;
            for(int cnt=0;cnt<count;cnt++)
            {
                json = JsonConvert.SerializeObject(entity, Settings);
                reconstructed = JsonConvert.DeserializeObject<First>(json, Settings);
            }
            var time = DateTime.Now - start;

            Console.WriteLine($"Completed {count} runs in: {time}");
            Console.WriteLine($"Average time: {time.TotalSeconds/count}");
        }
    }



    public class First : IMongoEntity<Guid>
    {
        public Guid Key { get; set; }

        public bool IsPersisted { get; set; }

        [JsonIgnore]
        object IMongoEntity.Key { get => Key; set => Key = (Guid)value; }

        public int Something { get; set; }
        public string Name { get; set; }


        public EntityRef<Second, Guid> Second { get; set; }
    }

    public class Second: IMongoEntity<Guid>
    {
        public Guid Key { get; set; }

        public bool IsPersisted { get; set; }

        [JsonIgnore]
        object IMongoEntity.Key { get => Key; set => Key = (Guid)value; }

        public DateTimeOffset TimeStamp { get; set; } = DateTimeOffset.Now;
    }
}
