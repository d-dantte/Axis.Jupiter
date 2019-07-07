using Axis.Jupiter.EFCore.ConsoleTest.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Axis.Jupiter.EFCore.ConsoleTest
{
    public class TestContext: DbContext
    {

        public DbSet<BioData> BioData { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }


        public TestContext(DbContextOptions options)
        : base(options)
        {
        }

        public TestContext()
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder = optionsBuilder.UseSqlServer("Data Source=(local); Initial Catalog=JupiterSample; User Id=dev; Password=G3n3r@t10n");

            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
