using Axis.Jupiter.Europa.Module;
using Sample.Core.Domain;
using Sample.Core.Mappers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Core
{
    public class ModuleConfig : BaseModuleConfigProvider
    {
        public override string ModuleName => typeof(ModuleConfig).FullName;

        protected override void Initialize()
        {
            this.UsingConfiguration(new PersonMapper())
                .UsingConfiguration(new ContactMapper())
                .UsingConfiguration(new MammalMapper())
                //.UsingConfiguration(new CourseMapper())
                .UsingConfiguration(new OnlineCourseMapper())
                .UsingConfiguration(new OnsiteCourseMapper())
                .UsingModelBuilder(builder =>
                {
                    builder.Entity<Course>()
                           .Property(c => c.Id)
                           .HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
                });

            //seed
            this.UsingContext(_cxt =>
            {
                var personStore = _cxt.Store<Person>();
                if (personStore.Query.Any()) return;
                //else

                personStore.Add(new Person
                {
                    DateOfBirth = DateTime.Now,
                    FirstName = "Donny",
                    LastName = "Brosco"
                })
                .Context.CommitChanges();
            });
        }
    }
}
