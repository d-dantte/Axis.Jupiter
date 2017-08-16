using Axis.Luna;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace Axis.Jupiter.Europa.Test
{
    [TestClass]
    public class DataContextTest
    {
        [TestMethod]
        public void BulkAddTest()
        {
            var cstring = "Data Source=(local);Initial Catalog=Europa_Test;User ID=sa;Password=developer;MultipleActiveResultSets=True;App=EntityFramework";

            var contextConfig = new ContextConfiguration<DataStore>()
                .UsingModule(new TestModuleConfig())
                .WithConnection(cstring)
                .WithInitializer(new DropCreateDatabaseIfModelChanges<DataStore>());


            using (var store = new DataStore(contextConfig))
            {
                store.Set<UserEntity>().Any();

                var bioList = new List<Bio>();
                var userList = new List<User>();
                for (int cnt = 0; cnt < 5000; cnt++)
                {
                    var user = new User
                    {
                        Status = 1,
                        UserId = $"@admin-{cnt+ 10000}"
                    };

                    var bio = new Bio
                    {
                        FirstName = RandomAlphaNumericGenerator.RandomAlpha(6),
                        LastName = RandomAlphaNumericGenerator.RandomAlpha(5),
                        Dob = DateTime.Now,
                        Owner = user
                    };

                    bioList.Add(bio);
                    userList.Add(user);
                }

                var p = userList;
                var start = DateTime.Now;
                store.AddBatch(p).Resolve();
                Console.WriteLine($"completed in: {DateTime.Now - start}");
            }
        }

        [TestMethod]
        public void BulkAddTest2()
        {
            var cstring = "Data Source=(local);Initial Catalog=Europa_Test;User ID=sa;Password=developer;MultipleActiveResultSets=True;App=EntityFramework";

            var contextConfig = new ContextConfiguration<DataStore>()
                .UsingModule(new TestModuleConfig())
                .WithConnection(cstring)
                .WithInitializer(new DropCreateDatabaseIfModelChanges<DataStore>());


            using (var store = new DataStore(contextConfig))
            {
                store.Set<UserEntity>().Any();

                var bioList = new List<Bio>();
                var userList = new List<User>();
                for (int cnt = 0; cnt < 20; cnt++)
                {
                    var user = new User
                    {
                        Status = 1,
                        UserId = $"@admin-{cnt}"
                    };

                    var bio = new Bio
                    {
                        FirstName = RandomAlphaNumericGenerator.RandomAlpha(6),
                        LastName = RandomAlphaNumericGenerator.RandomAlpha(5),
                        Dob = DateTime.Now,
                        Owner = user
                    };

                    bioList.Add(bio);
                    userList.Add(user);
                }
                
                var start = DateTime.Now;
                store.AddBatch(userList).Resolve();
                Console.WriteLine($"completed in: {DateTime.Now - start}");
            }
        }


        [TestMethod]
        public void BulkDeleteTest()
        {
            var cstring = "Data Source=(local);Initial Catalog=Europa_Test;User ID=sa;Password=developer;MultipleActiveResultSets=True;App=EntityFramework";

            var contextConfig = new ContextConfiguration<DataStore>()
                .UsingModule(new TestModuleConfig())
                .WithConnection(cstring)
                .WithInitializer(new DropCreateDatabaseIfModelChanges<DataStore>());

            using (var store = new DataStore(contextConfig))
            {
                store.Set<UserEntity>().Any();

                var bioList = new List<Bio>();
                var userList = new List<User>();
                for (int cnt = 0; cnt < 5000; cnt++)
                {
                    var user = new User
                    {
                        UserId = $"@admin-{cnt}"
                    };

                    userList.Add(user);
                }

                var start = DateTime.Now;
                store.DeleteBatch(userList).Resolve();
                Console.WriteLine($"deleted in {DateTime.Now - start}");
            }

        }


        [TestMethod]
        public void BulkUpdateTest()
        {
            var cstring = "Data Source=(local);Initial Catalog=Europa_Test;User ID=sa;Password=developer;MultipleActiveResultSets=True;App=EntityFramework";

            var contextConfig = new ContextConfiguration<DataStore>()
                .UsingModule(new TestModuleConfig())
                .WithConnection(cstring)
                .WithInitializer(new DropCreateDatabaseIfModelChanges<DataStore>());

            using (var store = new DataStore(contextConfig))
            {
                store.Set<UserEntity>().Any();

                var bioList = new List<Bio>();
                var userList = new List<User>();
                for (int cnt = 0; cnt < 5000; cnt++)
                {
                    var user = new User
                    {
                        UserId = $"@admin-{cnt}",
                        Status = 2
                    };

                    userList.Add(user);
                }

                var start = DateTime.Now;
                store.UpdateBatch(userList).Resolve();
                Console.WriteLine($"updated in {DateTime.Now - start}");
            }

        }
    }
}
