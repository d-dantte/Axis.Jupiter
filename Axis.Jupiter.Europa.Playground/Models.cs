using Axis.Jupiter.Europa.Mappings;
using Axis.Jupiter.Europa.Module;
using Axis.Luna.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Axis.Jupiter.Europa.Test
{

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
    public class User : Base
    {
        public int Status { get; set; }
        public string UserId { get; set; }
        public Bio Bio { get; set; }
        public ICollection<Contact> Contacts { get; set; }
    }
    public class Bio : Base
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }

        public DateTime Dob { get; set; }
        public string Nationality { get; set; }

        public virtual User Owner { get; set; }
    }
    public class Contact : Base
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

        public ZipCode ZipCode { get; set; }
    }

    public class ZipCode
    {
        public string Code { get; set; }
    }

    public class Shape : Base
    {
        public int SideCount { get; set; }
        public string Name { get; set; }

        public User Owner { get; set; }
    }
    public class Circle : Shape
    {
        public double Radius { get; set; }
    }
    public class Rectangle : Shape
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

    public class UserEntity : BaseEntity
    {
        public BioEntity Bio { get; set; }
        public ICollection<ContactEntity> Contacts { get; set; }
        public int Status { get; set; }
        public string UserId { get; set; }
    }

    public class BioEntity : BaseEntity
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }

        public DateTime Dob { get; set; }
        public string Nationality { get; set; }

        public string OwnerId { get; set; }
        public UserEntity Owner { get; set; }
    }

    public class BioXEntity : BaseEntity
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }

        public DateTime Dob { get; set; }
        public string Nationality { get; set; }

        public string ChemicalX { get; set; }
    }

    public class ContactEntity : BaseEntity
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
        public ZipCodeEntity ZipCode { get; set; }
    }

    public class ZipCodeEntity
    {
        public string Code { get; set; }
    }

    public class ShapeEntity : BaseEntity
    {
        public long StoreId { get; set; }
        new public Guid UUId { get; set; }
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

        public override void CopyToEntity(User model, UserEntity entity, ModelConverter converter)
        {
            entity.Bio = converter.ToEntity(model.Bio).Cast<BioEntity>();
            entity.Contacts = new HashSet<ContactEntity>(model.Contacts?.Select(_c => converter.ToEntity(_c).Cast<ContactEntity>()) ?? new ContactEntity[0]);
            entity.CreatedOn = model.CreatedOn;
            entity.Status = model.Status;
            entity.UserId = model.UserId;
            entity.UUId = model.UUId;
        }

        public override void CopyToModel(UserEntity entity, User model, ModelConverter converter)
        {
            model.Bio = converter.ToModel<Bio>(entity.Bio).Cast<Bio>();
            model.Contacts = new HashSet<Contact>(entity.Contacts?.Select(_c => converter.ToModel<Contact>(_c).Cast<Contact>()) ?? new Contact[0]);
            model.CreatedOn = entity.CreatedOn;
            model.Status = entity.Status;
            model.UserId = entity.UserId;
            model.UUId = entity.UUId;
        }

        //public override void ExportStoreMetadata(User model, string serializedMetadata)
        //{
        //    model.StoreMetadata = serializedMetadata;
        //}

        //public override string ImportStoreMetadata(User model) => model?.StoreMetadata;
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

        public override void CopyToEntity(Bio model, BioEntity entity, ModelConverter converter)
        {
            entity.CreatedOn = model.CreatedOn;
            entity.Dob = model.Dob;
            entity.FirstName = model.FirstName;
            entity.LastName = model.LastName;
            entity.Nationality = model.Nationality;
            entity.Owner = converter.ToEntity(model.Owner).Cast<UserEntity>();
            entity.UUId = model.UUId;
        }

        public override void CopyToModel(BioEntity entity, Bio model, ModelConverter converter)
        {
            model.CreatedOn = entity.CreatedOn;
            model.Dob = entity.Dob;
            model.FirstName = entity.FirstName;
            model.LastName = entity.LastName;
            model.Nationality = entity.Nationality;
            model.UUId = entity.UUId;
            model.Owner = converter.ToModel<User>(entity.Owner).Cast<User>();
        }
    }

    public class BioXMapping : BaseEntityMapConfig<Bio, BioXEntity>
    {
        public BioXMapping()
        {
            HasKey(m => m.UUId);
        }

        public override void CopyToEntity(Bio model, BioXEntity entity, ModelConverter converter)
        {
            entity.CreatedOn = model.CreatedOn;
            entity.Dob = model.Dob;
            entity.FirstName = model.FirstName;
            entity.LastName = model.LastName;
            entity.Nationality = model.Nationality;
            entity.UUId = model.UUId;
        }

        public override void CopyToModel(BioXEntity entity, Bio model, ModelConverter converter)
        {
            model.CreatedOn = entity.CreatedOn;
            model.Dob = entity.Dob;
            model.FirstName = entity.FirstName;
            model.LastName = entity.LastName;
            model.Nationality = entity.Nationality;
            model.UUId = entity.UUId;
        }

        //public override void ExportStoreMetadata(Bio model, string serializedMetadata)
        //{
        //    model.StoreMetadata = serializedMetadata;
        //}

        //public override string ImportStoreMetadata(Bio model) => model?.StoreMetadata;
    }

    public class ShapeMapping : BaseEntityMapConfig<Shape, ShapeEntity>
    {
        public ShapeMapping() : base(false)
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

        public override void CopyToEntity(Shape model, ShapeEntity entity, ModelConverter converter)
        {
            entity.CreatedOn = model.CreatedOn;
            entity.Name = model.Name;
            entity.Owner = converter.ToEntity(model.Owner).Cast<UserEntity>();
            entity.SideCount = model.SideCount;
            entity.UUId = model.UUId;
        }

        public override void CopyToModel(ShapeEntity entity, Shape model, ModelConverter converter)
        {
            model.CreatedOn = entity.CreatedOn;
            model.Name = entity.Name;
            model.Owner = converter.ToModel<User>(entity.Owner).Cast<User>();
            model.SideCount = entity.SideCount;
            model.UUId = entity.UUId;
        }

        //public override void ExportStoreMetadata(Shape model, string serializedMetadata)
        //{
        //    model.StoreMetadata = serializedMetadata;
        //}

        //public override string ImportStoreMetadata(Shape model) => model?.StoreMetadata;
    }

    public class AddressMapping : BaseComplexMapConfig<Address, AddressEntity>
    {
        public AddressMapping()
        {
        }

        public override void CopyToEntity(Address model, AddressEntity entity, ModelConverter converter)
        {
            entity.State = model.State;
            entity.Street = model.Street;
            entity.Town = model.Town;
            entity.ZipCode = converter.ToEntity(model.ZipCode).Cast<ZipCodeEntity>();
        }

        public override void CopyToModel(AddressEntity entity, Address model, ModelConverter converter)
        {
            model.State = entity.State;
            model.Street = entity.Street;
            model.Town = entity.Town;
            model.ZipCode = converter.ToModel<ZipCode>(entity.ZipCode).Cast<ZipCode>();
        }

        //public override void ExportStoreMetadata(Address model, string serializedMetadata)
        //{
        //}

        //public override string ImportStoreMetadata(Address model) => null;
    }

    public class ZipMapping : BaseComplexMapConfig<ZipCode, ZipCodeEntity>
    {
        public ZipMapping()
        {
        }

        public override void CopyToEntity(ZipCode model, ZipCodeEntity entity, ModelConverter converter)
        {
            entity.Code = model.Code;
        }
        public override void CopyToModel(ZipCodeEntity entity, ZipCode model, ModelConverter converter)
        {
            model.Code = entity.Code;
        }


        //public override void ExportStoreMetadata(ZipCode model, string serializedMetadata)
        //{
        //}
        //public override string ImportStoreMetadata(ZipCode model) => null;
    }

    public class ContactMapping : BaseEntityMapConfig<Contact, ContactEntity>
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

        public override void CopyToEntity(Contact model, ContactEntity entity, ModelConverter converter)
        {
            entity.Address = converter.ToEntity(model.Address).Cast<AddressEntity>();
            entity.CreatedOn = model.CreatedOn;
            entity.Email = model.Email;
            entity.Owner = converter.ToEntity(model.Owner).Cast<UserEntity>();
            entity.Phone = model.Phone;
            entity.Status = model.Status;
            entity.UUId = model.UUId;
        }

        public override void CopyToModel(ContactEntity entity, Contact model, ModelConverter converter)
        {
            model.Address = converter.ToModel<Address>(entity.Address).Cast<Address>();
            model.CreatedOn = entity.CreatedOn;
            model.Email = entity.Email;
            model.Owner = converter.ToModel<User>(entity.Owner).Cast<User>();
            model.Phone = entity.Phone;
            model.Status = entity.Status;
            model.UUId = entity.UUId;
        }

        //public override void ExportStoreMetadata(Contact model, string serializedMetadata)
        //{
        //    model.StoreMetadata = serializedMetadata;
        //}

        //public override string ImportStoreMetadata(Contact model) => model?.StoreMetadata;
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
                .UsingConfiguration(new BioXMapping())
                .UsingConfiguration(new ContactMapping())
                .UsingConfiguration(new AddressMapping())
                .UsingConfiguration(new ZipMapping())
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
                if (!userStore.Any()) userStore.Add(new UserEntity
                {
                    Status = 1,
                    UserId = "@root"
                });
            });
        }
    }
    #endregion
}
