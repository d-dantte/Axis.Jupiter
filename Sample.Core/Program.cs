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

        public static void Mainx(string[] args)
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

        public static void Main(string[] args)
        {
            new XYContext("server=(local);database=XYTest;integrated security=False;user id=sa;password=developer;multipleactiveresultsets=True").Using(context =>
            {
                //var x = new X();
                //x.Ys.Add(new Y());

                //context.Set<X>().Add(x);
                //context.SaveChanges();
            });

            new XYContext("server=(local);database=XYTest;integrated security=False;user id=sa;password=developer;multipleactiveresultsets=True").Using(context =>
            {
                var x = new X();
                x.Id = 1;
                context.Set<X>().Attach(x);

                x.Ys.Add(new Y());
                x.Ys.Add(new Y());
                x.Ys.Add(new Y());
                context.SaveChanges();
            });
        }
    }

    public class X
    {
        public long Id { get; set; }

        //public ICollection<XRef> Xrs { get; set; } = new HashSet<XRef>();
        public ICollection<Y> Ys { get; set; } = new HashSet<Y>();
    }

    public class XRef
    {
        public long Id { get; set; }

        public X X { get; set; }

        public string Code1 { get; set; }
        public string Code2 { get; set; }
    }

    public class Y
    {
        public long Id { get; set; }
        //public string Code { get; set; }
        

        public ICollection<X> Xs { get; set; } = new HashSet<X>();

    }

    public class XYContext: DbContext
    {
        public XYContext(string c) : base(c)
        { }

        public DbSet<X> XSet { get; set; }
        public DbSet<XRef> XRefSet { get; set; }
        public DbSet<Y> YSet { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<X>()
                .UsingValue(xconfig =>
                {
                    xconfig.HasMany(x => x.Ys)
                           .WithMany(y => y.Xs);
                })
                .Pipe(xconfig => modelBuilder.Configurations.Add(xconfig));

            modelBuilder.Configurations.Add(modelBuilder.Entity<XRef>().UsingValue(xrconfig =>
            {
                
            }));
        }
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
