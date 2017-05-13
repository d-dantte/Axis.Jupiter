using Axis.Jupiter.Europa;
using Axis.Jupiter.Europa.Mappings;
using Sample.Core.Domain;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sample.Core.Mappers
{
    public class PersonMapper: BaseMap<Person>
    {
        public PersonMapper()
        {
            ToTable("__Person__X");
            Property(e => e.FirstName).HasMaxLength(250);
            Property(e => e.LastName).HasColumnName("ABCD").HasMaxLength(250);

            this.HasMany(e => e.ContactInfo)
                .WithRequired(e => e.Owner)
                .HasForeignKey(e => e.OwnerId);
        }
    }

    public class ContactMapper: BaseMap<Contact>
    {
        public ContactMapper()
        {
            Property(e => e.Address).HasMaxLength(500);
            Property(e => e.Phone).HasMaxLength(500);
            Property(e => e.Email).HasMaxLength(500).IsIndex("ABC_Index");
        }
    }

    public class WebSiteMapper : BaseComplexMap<WebSite>
    {
        public WebSiteMapper()
        {
            ///Conifgure properties
            this.Property(c => c.Host)
                .HasColumnName($"Meta__H0st");

            this.Property(c => c.Page)
                .HasColumnName($"Meta__P@ge");
        }
    }

    public class MammalMapper: BaseMap<Mammal>
    {
        public MammalMapper()
        {
            this.Property(e => e.Id).HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
            this.Map<Cat>(m => m.MapInheritedProperties().Requires("Type").HasValue("Cat"));
            this.Map<Dog>(m => m.MapInheritedProperties().Requires("Type").HasValue("Dog"));
        }
    }

    
    public class OnlineCourseMapper:BaseMap<OnlineCourse>
    {
        public OnlineCourseMapper():base(false)
        {
            Map(m =>
            {
                m.MapInheritedProperties();
                m.ToTable("OnlineCourse");
            });
        }
    }
    public class OnsiteCourseMapper: BaseMap<OnsiteCourse>
    {
        public OnsiteCourseMapper():base(false)
        {
            Map(m =>
            {
                m.MapInheritedProperties();
                m.ToTable("OnsiteCourse");
            });
        }
    }
}
