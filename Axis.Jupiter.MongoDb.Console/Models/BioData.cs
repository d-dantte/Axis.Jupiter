using System;

namespace Axis.Jupiter.MongoDb.ConsoleTest.Models
{
    public class BioData
    {
        public User Owner { get; set; }

        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }

        public Sex Sex { get; set; }

        public DateTimeOffset DateOfBirth { get; set; }
        public string Nationality { get; set; }
    }

    public enum Sex
    {
        Male,
        Female,
        Other
    }
}
