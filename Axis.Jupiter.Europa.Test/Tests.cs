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
                var users = new List<User>();
                for(int cnt=0;cnt<10;cnt++)
                {
                    users.Add(new User
                    {
                        Status = r.Next(10),
                        UserId = $"{RandomAlphaNumericGenerator.RandomAlpha(5)}@{RandomAlphaNumericGenerator.RandomAlpha(4)}.com",
                        Bio = new Bio
                        {
                            FirstName = RandomAlphaNumericGenerator.RandomAlpha(5),
                            LastName = RandomAlphaNumericGenerator.RandomAlpha(5),
                            Nationality = RandomAlphaNumericGenerator.RandomAlpha(5),
                            Dob = DateTime.Now
                        },
                        Contacts = new HashSet<Contact>(new[]
                        {
                            new Contact
                            {
                                Address = new Address
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
                    });
                }

                var randomUserId = RandomAlphaNumericGenerator.RandomAlpha(5);
                var x = store.MappingFor<UserEntity>();
                var start = DateTime.Now;
                var ue = store.TransformEntity<UserEntity, User>(new UserEntity { UserId =  randomUserId}, new Dictionary<object, object>());
                Console.WriteLine($"First conversion done in: {DateTime.Now - start}");

                start = DateTime.Now;
                for(int cnt=0;cnt<100;cnt++)
                ue = store.TransformEntity<UserEntity, User>(new UserEntity { UserId = randomUserId }, new Dictionary<object, object>());
                Console.WriteLine($"100 conversions done in: {DateTime.Now - start}");
            }
        }
    }

    #region Models
    public abstract class Base
    {
        public Guid UUId { get; set; }
        public DateTime CreatedOn { get; set; }
        
        public string StoreMetadata { get; set; }

        public Base()
        {
            CreatedOn = DateTime.Now;
            UUId = Guid.NewGuid();
        }
    }
    public class User: Base
    {
        public int Status { get; set; }
        public string UserId { get; set; }
        public Bio Bio { get; set; }
        public ICollection<Contact> Contacts { get; set; }
    }
    public class Bio: Base
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }

        public DateTime Dob { get; set; }
        public string Nationality { get; set; }

        public virtual User Owner { get; set; }
    }
    public class Contact: Base
    {
        public string Phone { get; set; }
        public string Email { get; set; }
        public int Status { get; set; }

        public Address Address { get; set; }

        public virtual User Owner { get; set; }
    }

    public class Address
    {
        public string Street { get; set; }
        public string Town { get; set; }
        public string State { get; set; }
    }

    public class Shape
    {
        public Guid UUId { get; set; }
        public int SideCount { get; set; }
        public string Name { get; set; }

        //public User Owner { get; set; }
    }
    public class Circle:Shape
    {
        public double Radius { get; set; }
    }
    public class Rectangle: Shape
    {
        public double Length { get; set; }
        public double Breath { get; set; }

        //public User Owner { get; set; }
    }
    #endregion


    #region Entities
    public abstract class BaseEntity
    {
        public Guid UUId { get; set; }
        public DateTime CreatedOn { get; set; }

        public BaseEntity()
        {
            CreatedOn = DateTime.Now;
            UUId = Guid.NewGuid();
        }
    }

    public class UserEntity : Base
    {
        public BioEntity Bio { get; set; }
        public ICollection<ContactEntity> Contacts { get; set; }
        public int Status { get; set; }
        public string UserId { get; set; }
    }

    public class BioEntity : Base
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }

        public DateTime Dob { get; set; }
        public string Nationality { get; set; }
        
        public string OwnerId { get; set; }
        public UserEntity Owner { get; set; }
    }

    public class ContactEntity : Base
    {
        public long StoreId { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public int Status { get; set; }

        public AddressEntity Address { get; set; }

        public UserEntity Owner { get; set; }
        public string OwnerId { get; set; }
    }

    public class AddressEntity
    {
        public string Street { get; set; }
        public string Town { get; set; }
        public string State { get; set; }
    }

    public class ShapeEntity
    {
        public long StoreId { get; set; }
        public Guid UUId { get; set; }
        public int SideCount { get; set; }
        public string Name { get; set; }

        public UserEntity Owner { get; set; }
        public string OwnerId { get; set; }
    }
    public class CircleEntity : ShapeEntity
    {
        public double Radius { get; set; }
    }
    public class RectangleEntity : ShapeEntity
    {
        public double Length { get; set; }
        public double Breath { get; set; }
    }
    #endregion


    #region Mappings

    public class UserMapping : BaseEntityMapConfig<User, UserEntity>
    {
        public UserMapping()
        {
            HasKey(m => m.UserId);

            Property(m => m.UUId)
                .IsIndex("User_UUID", true);

            HasOptional(m => m.Bio)
                .WithRequired(m => m.Owner);
        }
    }

    public class BioMapping : BaseEntityMapConfig<Bio, BioEntity>
    {
        public BioMapping()
        {
            HasKey(m => m.OwnerId);

            Property(m => m.UUId)
                .IsIndex("Bio_UUID", true);

            HasRequired(m => m.Owner)
                .WithOptional(m => m.Bio);
        }
    }

    public class ShapeMapping : BaseEntityMapConfig<Shape, ShapeEntity>
    {
        public ShapeMapping(): base(false)
        {
            ToTable("Shapes__");

            HasKey(m => m.StoreId);

            Property(m => m.UUId)
                .IsIndex("Shape_UUID", true);

            HasRequired(m => m.Owner)
                .WithMany()
                .HasForeignKey(m => m.OwnerId);
            
            Map<CircleEntity>(m => m.Requires("Type").HasValue("Circle"));
            Map<RectangleEntity>(m => m.Requires("Type").HasValue("Rect"));
        }
    }

    public class AddressMapping: BaseComplexMapConfig<Address, AddressEntity>
    {
        public AddressMapping()
        {
        }
    }

    public class ContactMapping: BaseEntityMapConfig<Contact, ContactEntity>
    {
        public ContactMapping()
        {
            HasKey(m => m.StoreId);

            Property(m => m.UUId)
                .IsIndex("Contact_UUID", true);

            HasRequired(m => m.Owner)
                .WithMany(m => m.Contacts)
                .HasForeignKey(m => m.OwnerId);
        }
    }
    #endregion


    #region ModuleConfig
    public class TestModuleConfig : ModuleConfigProvider
    {
        public TestModuleConfig() 
        : base(nameof(TestModuleConfig))
        {
            this.UsingConfiguration(new UserMapping())
                .UsingConfiguration(new BioMapping())
                .UsingConfiguration(new ContactMapping())
                .UsingConfiguration(new AddressMapping())
                .UsingConfiguration(new ShapeMapping());

            //general model builder configurations
            this.UsingModelBuilder(_mb =>
            {
                var baseMap = _mb.Entity<Base>();
                baseMap.Ignore(_m => _m.StoreMetadata);

                _mb.Ignore<Base>();
            });

            //naturally, seeding data comes here. lets seed with a root-user
            this.WithContextAction(store =>
            {
                var userStore = store.Set<UserEntity>();
                if(!userStore.Any()) userStore.Add(new UserEntity
                {
                    Status = 1,
                    UserId = "@root"
                });
            });
        }
    }
    #endregion
}
