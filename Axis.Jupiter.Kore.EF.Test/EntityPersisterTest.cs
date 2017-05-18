using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration;
using System.Data.SqlClient;
using Axis.Jupiter.Europa.Module;
using Axis.Jupiter.Europa;
using System.Collections.Generic;
using Axis.Luna;

namespace Axis.Jupiter.Kore.EF.Test
{
    [TestClass]
    public class EntityPersisterTest
    {
        [TestMethod]
        public void TestMethod1()
        {
            var cstring = "Data Source=(local);Initial Catalog=Kore_EF_Test;User ID=sa;Password=developer;MultipleActiveResultSets=True;App=EntityFramework";

            var moduleConfig = new ModuleConfigProvider();
            moduleConfig.UsingConfiguration(new EntityTypeConfiguration<SomeClass>());

            var contextConfig = new ContextConfiguration<DataStore>()
                .UsingModule(moduleConfig)
                .WithConnection(cstring)
                .WithInitializer(new DropCreateDatabaseIfModelChanges<DataStore>());

            //using (var cxt = new DbContext("Context", mb.Build(new SqlConnection(cstring)).Compile()))
            using(var cxt = new DataStore(contextConfig))
            {
                var obj = new SomeClass { Id = 1 };
                var _obj = new SomeClass { Id = 1 };
                var set = cxt.Set<SomeClass>();

                set.Attach(obj);

                var entry = cxt.Entry(_obj);

                Console.WriteLine(entry.State);

                Console.WriteLine(entry.Entity == obj);
                Console.WriteLine(entry.Entity == _obj);

                try
                {
                    entry.State = EntityState.Modified;
                }
                catch(InvalidOperationException ioe)
                {
                    Console.WriteLine("caught the devil");
                    throw ioe;
                }
            }
        }

        [TestMethod]
        public void UpdateTest()
        {
            var cstring = "Data Source=(local);Initial Catalog=Kore_EF_Test;User ID=sa;Password=developer;MultipleActiveResultSets=True;App=EntityFramework";

            var moduleConfig = new ModuleConfigProvider();
            moduleConfig.UsingConfiguration(new EntityTypeConfiguration<SomeClass>());

            var contextConfig = new ContextConfiguration<DataStore>()
                .UsingModule(moduleConfig)
                .WithConnection(cstring)
                .WithInitializer(new DropCreateDatabaseIfModelChanges<DataStore>());

            using (var persister = new EntityPersister(new DataStore(contextConfig)))
            {
                var obj = new SomeClass { Data = $"First Data {DateTime.Now.Ticks}" };
                persister.Add(obj)
                         .Then(_d => Console.WriteLine("Persisted first"),
                               _er => Console.WriteLine($"Didnt persist {_er}"))
                         .Resolve();

                var _obj = new SomeClass { Data = "Next data", Id = obj.Id };
                persister.Update(_obj)
                         .Then(_d => Console.WriteLine("Updated second"),
                               _er => Console.WriteLine($"Didnt persist {_er}"))
                         .Resolve();
            }

        }

        [TestMethod]
        public void BulkInsertTest()
        {
            var cstring = "Data Source=(local);Initial Catalog=Kore_EF_Test;User ID=sa;Password=developer;MultipleActiveResultSets=True;App=EntityFramework";

            var moduleConfig = new ModuleConfigProvider();
            moduleConfig.UsingConfiguration(new EntityTypeConfiguration<SomeClass>());

            var contextConfig = new ContextConfiguration<DataStore>()
                .UsingModule(moduleConfig)
                .WithConnection(cstring)
                .WithInitializer(new DropCreateDatabaseIfModelChanges<DataStore>());

            using (var persister = new EntityPersister(new DataStore(contextConfig)))
            {
                //generate objects to insert
                var objects = new List<SomeClass>();
                for (int cnt = 0; cnt < 10000; cnt++)
                {
                    objects.Add(new SomeClass
                    {
                        Data = RandomAlphaNumericGenerator.RandomAlpha(10)
                    });
                }

                //persist them in bulk
                var start = DateTime.Now;
                var resolved = persister.AddBatch(objects).Resolve();
                Console.WriteLine($"Executed in {DateTime.Now - start}");
            }
        }
    }


    public class SomeClass
    {
        public int Id { get; set; }
        public string Data { get; set; }
    }
}
