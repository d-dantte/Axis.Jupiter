using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Axis.Jupiter.Europa.Mappings;
using Axis.Luna;
using System.Linq;
using Axis.Luna.Extensions;
using Axis.Jupiter.Europa.Module;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Xml;
using System.Text;
using System.Data.SqlClient;
using System.Collections.Generic;
using Axis.Jupiter.Europa.Utils;

namespace Axis.Jupiter.Europa.Test
{
    [TestClass]
    public class Tests
    {
        [TestMethod]
        public void TestMethod1()
        {
            var cstring = "Data Source=(local);Initial Catalog=Europa_Test;User ID=sa;Password=developer;MultipleActiveResultSets=True;App=EntityFramework";
            
            var contextConfig = new ContextConfiguration<DataStore>()
                .UsingModule(new TestModuleConfig())
                .WithConnection(cstring)
                .WithInitializer(new DropCreateDatabaseIfModelChanges<DataStore>());
            
            using (var store = new DataStore(contextConfig))
            {
                store.Set<UserEntity>().Any(); //<-- force db creation
            }
        }

        [TestMethod]
        public void TestInsert()
        {
            var cstring = "Data Source=(local);Initial Catalog=Europa_Test;User ID=sa;Password=developer;MultipleActiveResultSets=True;App=EntityFramework";

            var contextConfig = new ContextConfiguration<DataStore>()
                .UsingModule(new TestModuleConfig())
                .WithConnection(cstring)
                .WithInitializer(new DropCreateDatabaseIfModelChanges<DataStore>());
            

            using (var store = new DataStore(contextConfig))
            {
                for (int cnt = 0; cnt < 100; cnt++)
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
                    var contact = new Contact
                    {
                        Email = $"admin-{cnt}@abc.xyz",
                        Phone = $"0{RandomAlphaNumericGenerator.RandomNumeric(10)}",
                        Owner = user
                    };

                    store.Add(bio).Resolve();
                    store.Add(contact).Resolve();
                }
            }
        }

        [TestMethod]
        public void GenerateEdmx()
        {
            var cstring = "Data Source=(local);Initial Catalog=Europa_Test;User ID=sa;Password=developer;MultipleActiveResultSets=True;App=EntityFramework";

            var contextConfig = new ContextConfiguration<DataStore>()
                .UsingModule(new TestModuleConfig())
                .WithConnection(cstring)
                .WithInitializer(new DropCreateDatabaseIfModelChanges<DataStore>());

            var builder = new DbModelBuilder(DbModelBuilderVersion.Latest);
            contextConfig.ConfiguredModules.ForAll(_t => _t.BuildModel(builder));
            var model = builder.Build(new SqlConnection(cstring));
            var start = DateTime.Now;
            var efm = new EFMappings(model);
            Console.WriteLine($"Completed in ~ {DateTime.Now - start}");

            using (var store = new DataStore(contextConfig))
            {
                new XmlTextWriter("Edmx.xml", Encoding.Default).Using(_writer => EdmxWriter.WriteEdmx(store, _writer));
            }
        }

        [TestMethod]
        public void ObjectTransformationTest()
        {
            var cstring = "Data Source=(local);Initial Catalog=Europa_Test;User ID=sa;Password=developer;MultipleActiveResultSets=True;App=EntityFramework";

            var contextConfig = new ContextConfiguration<DataStore>()
                .UsingModule(new TestModuleConfig())
                .WithConnection(cstring)
                .WithInitializer(new DropCreateDatabaseIfModelChanges<DataStore>());

            var r = new Random();
            using (var store = new DataStore(contextConfig))
            {
                var randomUserId = RandomAlphaNumericGenerator.RandomAlpha(5);
                var x = store.MappingFor<UserEntity>();
                var start = DateTime.Now;
                var _ue = new UserEntity
                {
                    UserId = randomUserId,
                    Contacts = new HashSet<ContactEntity>(new[]
                        {
                            new ContactEntity
                            {
                                Address = new AddressEntity
                                {
                                    State = RandomAlphaNumericGenerator.RandomAlpha(5),
                                    Street = RandomAlphaNumericGenerator.RandomAlpha(5),
                                    Town = RandomAlphaNumericGenerator.RandomAlpha(5)
                                },
                                Email = RandomAlphaNumericGenerator.RandomAlpha(5),
                                Phone = RandomAlphaNumericGenerator.RandomAlpha(5),
                                Status = r.Next(10)
                            }
                        })
                };
                var _bioe = new BioEntity
                {
                    FirstName = RandomAlphaNumericGenerator.RandomAlpha(5),
                    LastName = RandomAlphaNumericGenerator.RandomAlpha(5),
                    Nationality = RandomAlphaNumericGenerator.RandomAlpha(5),
                    Dob = DateTime.Now,
                    Owner = _ue
                };
                _ue.Bio = _bioe;
                //var ue = store.TransformEntity<UserEntity, User>(_ue, new Dictionary<object, object>());
                Console.WriteLine($"First conversion done in: {DateTime.Now - start}");

                start = DateTime.Now;
                var limit = 100000;
                for (int cnt = 0; cnt < limit; cnt++)
                {
                    _ue = new UserEntity
                    {
                        UserId = randomUserId,
                        Contacts = new HashSet<ContactEntity>(new[]
                           {
                            new ContactEntity
                            {
                                Address = new AddressEntity
                                {
                                    State = " ;dflkeaf lkfja;elkjfll",
                                    Street = " ;dflkeaf lkfja;elkjfll",
                                    Town = " ;dflkeaf lkfja;elkjfll"
                                },
                                Email = " ;dflkeaf lkfja;elkjfll",
                                Phone = " ;dflkeaf lkfja;elkjfll",
                                Status = r.Next(10)
                            }
                        })
                    };
                    _bioe = new BioEntity
                    {
                        FirstName = " ;dflkeaf lkfja;elkjfll",
                        LastName = " ;dflkeaf lkfja;elkjfll",
                        Nationality = " ;dflkeaf lkfja;elkjfll",
                        Dob = DateTime.Now,
                        Owner = _ue
                    };
                    _ue.Bio = _bioe;
                    //ue = store.TransformEntity<UserEntity, User>(_ue, new Dictionary<object, object>());
                }
                Console.WriteLine($"{limit} conversions done in: {DateTime.Now - start}");
            }
        }

        [TestMethod]
        public void ModelConverterTest()
        {
            var cstring = "Data Source=(local);Initial Catalog=Europa_Test;User ID=sa;Password=developer;MultipleActiveResultSets=True;App=EntityFramework";

            var contextConfig = new ContextConfiguration<DataStore>()
                .UsingModule(new TestModuleConfig())
                .WithConnection(cstring)
                .WithInitializer(new DropCreateDatabaseIfModelChanges<DataStore>());
            
            using (var store = new DataStore(contextConfig))
            {
                var converter = new ModelConverter(store);
                var ue = GenerateUserEntity();

                var start = DateTime.Now;
                var u = converter.ToModel<User>(ue).Cast<User>();
                Console.WriteLine($"First conversion done in {DateTime.Now - start}");

                var limit = 100000;
                start = DateTime.Now;
                for(int cnt=0;cnt<limit;cnt++)
                {
                    ue = GenerateUserEntity();
                    u = converter.ToModel<User>(ue).Cast<User>();
                }

                Console.WriteLine($"{limit} conversions done in {DateTime.Now - start}");
            }
        }

        private UserEntity GenerateUserEntity()
        {
            var _ue = new UserEntity
            {
                UserId = " ;dflkeaf lkfja;elkjfll",
                Contacts = new HashSet<ContactEntity>(new[]
                        {
                            new ContactEntity
                            {
                                Address = new AddressEntity
                                {
                                    State = " ;dflkeaf lkfja;elkjfll",
                                    Street = " ;dflkeaf lkfja;elkjfll",
                                    Town = " ;dflkeaf lkfja;elkjfll"
                                },
                                Email = " ;dflkeaf lkfja;elkjfll",
                                Phone = " ;dflkeaf lkfja;elkjfll",
                                Status = 5
                            }
                        })
            };
            var _bioe = new BioEntity
            {
                FirstName = " ;dflkeaf lkfja;elkjfll",
                LastName = " ;dflkeaf lkfja;elkjfll",
                Nationality = " ;dflkeaf lkfja;elkjfll",
                Dob = DateTime.Now,
                Owner = _ue
            };
            _ue.Bio = _bioe;

            return _ue;
        }


        [TestMethod]
        public void MiscTest()
        {
            using (var dbContext = new DbContext("Data Source=(local);Initial Catalog=Europa_Test;User ID=sa;Password=developer;MultipleActiveResultSets=True;App=EntityFramework"))
            {

                var q = dbContext.Database.SqlQuery<int?>("SELECT OBJECT_ID('dbo.listToTable')");
                Console.WriteLine(q.FirstOrDefault());

                #region Create Function Statement
                var createFunction = @"
CREATE FUNCTION listToTable
                 (@list      nvarchar(MAX),
                  @delimiter nchar(1) = N',')
      RETURNS @tbl TABLE (listpos int IDENTITY(1, 1) NOT NULL,
                          str     varchar(4000)      NOT NULL) AS

BEGIN
   DECLARE @endpos   int,
           @startpos int,
           @textpos  int,
           @chunklen smallint,
           @tmpstr   nvarchar(4000),
           @leftover nvarchar(4000),
           @tmpval   nvarchar(4000)

   SET @textpos = 1
   SET @leftover = ''
   WHILE @textpos <= datalength(@list) / 2
   BEGIN
      SET @chunklen = 4000 - datalength(@leftover) / 2
      SET @tmpstr = @leftover + substring(@list, @textpos, @chunklen)
      SET @textpos = @textpos + @chunklen

      SET @startpos = 0
      SET @endpos = charindex(@delimiter COLLATE Slovenian_BIN2, @tmpstr)

      WHILE @endpos > 0
      BEGIN
         SET @tmpval = ltrim(rtrim(substring(@tmpstr, @startpos + 1,
                                             @endpos - @startpos - 1)))
         INSERT @tbl (str) VALUES(@tmpval)
         SET @startpos = @endpos
         SET @endpos = charindex(@delimiter COLLATE Slovenian_BIN2,
                                 @tmpstr, @startpos + 1)
      END

      SET @leftover = right(@tmpstr, datalength(@tmpstr) / 2 - @startpos)
   END

   INSERT @tbl(str)
      VALUES (ltrim(rtrim(@leftover)))
   RETURN
END
";
                #endregion
                var r = dbContext.Database.ExecuteSqlCommand(createFunction);
                Console.WriteLine(r);

                q = dbContext.Database.SqlQuery<int?>("SELECT OBJECT_ID('dbo.listToTable')");
                Console.WriteLine(q.FirstOrDefault());
            }
        }

        [TestMethod]
        public void MiscTest2()
        {
            using (var dbContext = new DbContext("Data Source=(local);Initial Catalog=EuropaTest;User ID=sa;Password=developer;MultipleActiveResultSets=True;App=EntityFramework"))
            {
                var q = dbContext.Database.ExecuteSqlCommand("select * into #abc1 from Contact where 1 = 0; select * into #abc2 from Mammal where 1 = 0;");
            }
        }
    }
}
