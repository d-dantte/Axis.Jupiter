using Axis.Jupiter.Europa;
using Axis.Luna;
using Axis.Luna.Extensions;
using Sample.Core.Domain;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Core
{
    public class Program
    {

        public static void Main(string[] args)
            => Operation.Try(() =>
            {
                var config = 
                    new ContextConfiguration<EuropaContext>()
                    //new ContextConfiguration<Context>()
                    .WithConnection("server=(local);database=EuropaTest;integrated security=False;user id=sa;password=developer;multipleactiveresultsets=True;")
                    //.WithInitializer(new DropCreateDatabaseIfModelChanges<Context>())
                    .WithInitializer(new DropCreateDatabaseIfModelChanges<EuropaContext>())
                    .UsingModule(new ModuleConfig());

                var cxt = new EuropaContext(config);
                var newCat = new Cat
                {
                    Feline = RandomAlphaNumericGenerator.RandomAlpha(6),
                    Weight = (float)new Random().NextDouble()
                };
                cxt.BulkInsert(newCat.Enumerate()).Wait();
                

                if (false)
                {
                    var sqlbulk = new SqlBulkCopy("server=(local);database=EuropaTest;integrated security=False;user id=sa;password=developer;multipleactiveresultsets=True;");
                    sqlbulk.DestinationTableName = "Mammal";
                    var props = typeof(Cat).GetProperties().OrderBy(_prop => _prop.Name).ToArray();
                    var table = new DataTable();
                    props.ForAll((__cnt, __next) =>
                    {
                        table.Columns.Add(__next.Name, Nullable.GetUnderlyingType(__next.PropertyType) ?? __next.PropertyType);
                        sqlbulk.ColumnMappings.Add(__next.Name, __next.Name);
                    });

                    var values = new object[props.Length];
                    foreach (var item in newCat.Enumerate())
                    {
                        for (var i = 0; i < values.Length; i++) values[i] = props[i].GetValue(item);
                        table.Rows.Add(values);
                    }
                    sqlbulk.WriteToServer(table);
                }

                //var p = cxt.People.FirstOrDefault();
                cxt.Store<Mammal>().Query.OfType<Cat>().ForAll((cnt, xyz) => Console.WriteLine($"{xyz.Id}, {xyz.Weight}, {xyz.Feline}"));
            })
            .Then(opr =>
            {
                Console.WriteLine("Done!");
                Console.ReadKey();
            })
            .Instead(opr =>
            {
                Console.WriteLine("An error occured!\n" + opr.GetException().FlattenMessage("\n"));
            });
    }

    public class Context: DbContext
    {
        public Context(ContextConfiguration<Context> xyz) : base(xyz.Compile())
        {
        }

        public Context(DbCompiledModel cm): base(cm)
        {
            Console.WriteLine("Database exists? "+Database.Exists());
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }

        public DbSet<Cat> People { get; set; }
    }
}
