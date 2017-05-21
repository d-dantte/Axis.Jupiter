using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Axis.Jupiter.Europa.Mappings;
using Axis.Luna;
using System.Linq;
using Axis.Luna.Extensions;
using Axis.Jupiter.Europa.Module;
using System.Data.Entity;
using System.ComponentModel.DataAnnotations.Schema;

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
                var userSet = store.Set<UserEntity>();
                var root = userSet.FirstOrDefault();

                var bioSet = store.Set<BioEntity>();
                var bio = bioSet
                    .Include(_ => _.Owner)
                    .FirstOrDefault(_bio => _bio.Owner.UserId == root.UserId)
                    ?? bioSet.Add(new BioEntity
                    {
                        Dob = DateTime.Now,
                        FirstName = "Root",
                        LastName = "Admin",
                        Nationality = "Blue Nowhere",
                        Owner = root
                    })
                    .UsingValue(_ => store.SaveChanges());
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

        public virtual User Owner { get; set; }
    }
    #endregion


    #region Entities
    public class UserEntity: User
    {
    }

    public class BioEntity: Bio
    {
        public long StoreId { get; set; }

        private UserEntity _owner;
        public new UserEntity Owner
        {
            get { return _owner; }
            set { base.Owner = _owner = value; }
        }
    }

    public class ContactEntity: Contact
    {
        public long StoreId { get; set; }
        public string OwnerId { get; set; }


        private UserEntity _owner;
        public new UserEntity Owner
        {
            get { return _owner; }
            set { base.Owner = _owner = value; }
        }
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
        }

        public override void EntityToModel(ModelConverter converter, UserEntity entity)
        {
        }

        public override void ModelToEntity(ModelConverter converter, User model, UserEntity entity)
        {
            model.CopyTo(entity);
        }
    }

    public class BioMapping : BaseEntityMapConfig<Bio, BioEntity>
    {
        public BioMapping()
        {
            HasKey(m => m.StoreId);

            Property(m => m.UUId)
                .IsIndex("Bio_UUID", true);

            HasRequired(m => m.Owner)
                .WithOptional();
        }

        public override void EntityToModel(ModelConverter converter, BioEntity entity)
        {
            var tbuilder = TagBuilder.Create();
            tbuilder.Add(nameof(BioEntity.StoreId), entity.StoreId.ToString());
            entity.StoreMetadata = tbuilder.ToString();
        }

        public override void ModelToEntity(ModelConverter converter, Bio model, BioEntity entity)
        {
            model.CopyTo(entity);

            var tbuilder = TagBuilder.Create(entity.StoreMetadata);
            entity.StoreId = Convert.ToInt64(tbuilder.Tags.FirstOrDefault(_t => _t.Name == nameof(BioEntity.StoreId))?.Value ?? "0");
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
                .WithMany()
                .HasForeignKey(m => m.OwnerId);
        }

        public override void EntityToModel(ModelConverter converter, ContactEntity entity)
        {
            var tbuilder = TagBuilder.Create();
            tbuilder.Add(nameof(ContactEntity.StoreId), entity.StoreId.ToString())
                    .Add(nameof(ContactEntity.OwnerId), entity.OwnerId.ToString());
            entity.StoreMetadata = tbuilder.ToString();
        }

        public override void ModelToEntity(ModelConverter converter, Contact model, ContactEntity entity)
        {
            model.CopyTo(entity);

            var tbuilder = TagBuilder.Create(entity.StoreMetadata);
            entity.StoreId = Convert.ToInt64(tbuilder.Tags.FirstOrDefault(_t => _t.Name == nameof(ContactEntity.StoreId))?.Value ?? "0");
            entity.OwnerId = tbuilder.Tags.FirstOrDefault(_t => _t.Name == nameof(ContactEntity.OwnerId))?.Value;
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
                .UsingConfiguration(new ContactMapping());

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
