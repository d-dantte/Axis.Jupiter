using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration;
using Axis.Jupiter.Europa.Module;
using Axis.Jupiter.Europa;
using System.Collections.Generic;
using Axis.Luna;
using System.Linq;
using Axis.Luna.Operation;

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
        public void BulkInsert()
        {
            var cstring = "Data Source=(local);Initial Catalog=Kore_EF_Test;User ID=sa;Password=developer;MultipleActiveResultSets=True;App=EntityFramework";

            var moduleConfig = new ModuleConfigProvider();
            moduleConfig.UsingConfiguration(new EntityTypeConfiguration<SomeClass>());

            var contextConfig = new ContextConfiguration<DataStore>()
                .UsingModule(moduleConfig)
                .WithConnection(cstring)
                .WithInitializer(new DropCreateDatabaseIfModelChanges<DataStore>());

            var objects = new List<SomeClass>();
            var store = new DataStore(contextConfig);
            using (var persister = new EntityPersister(store))
            {
                //generate objects to insert
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
                Console.WriteLine($"Inserted in {DateTime.Now - start}");

                //delete bulk
                start = DateTime.Now;
                resolved = persister.DeleteBatch(objects).Resolve();
                Console.WriteLine($"Deleted in {DateTime.Now - start}");

                //make sure it's deleted
                objects.ForEach(_obj =>
                {
                    Assert.IsFalse(store.Set<SomeClass>().Any(_x => _x.Id == _obj.Id));
                });

            }
        }

        [TestMethod]
        public void DeleteTest()
        {
            var cstring = "Data Source=(local);Initial Catalog=Kore_EF_Test;User ID=sa;Password=developer;MultipleActiveResultSets=True;App=EntityFramework";

            var moduleConfig = new ModuleConfigProvider();
            moduleConfig.UsingConfiguration(new EntityTypeConfiguration<SomeClass>());

            var contextConfig = new ContextConfiguration<DataStore>()
                .UsingModule(moduleConfig)
                .WithConnection(cstring)
                .WithInitializer(new DropCreateDatabaseIfModelChanges<DataStore>());

            var objects = new List<SomeClass>();
            var store = new DataStore(contextConfig);
            using (var persister = new EntityPersister(store))
            {
                //delete single
                var obj = new SomeClass { Id = 1 };
                var op = ResolvedOp.Try(() => persister.Delete(obj));
                
                Assert.AreEqual(1, op.Resolve().Id);


                //bulk
                for (int cnt = 10000; cnt < 20000; cnt++) objects.Add(new SomeClass { Id = cnt });
                var _op = ResolvedOp.Try(() => persister.DeleteBatch(objects));

                Assert.AreEqual(true, _op.Succeeded);
            }
        }
    }


    public class SomeClass
    {
        public int Id { get; set; }
        public string Data { get; set; }
    }
}
