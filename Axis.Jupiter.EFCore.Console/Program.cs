using Axis.Jupiter.Configuration;
using Axis.Jupiter.Helpers;
using Axis.Jupiter.Models;
using Axis.Jupiter.Providers;
using Axis.Jupiter.Services;
using Axis.Luna.Extensions;
using Axis.Proteus.Ioc;
using Axis.Proteus.SimpleInjector;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Axis.Jupiter.EFCore.ConsoleTest
{
    class Program
    {
        readonly static JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            Formatting = Formatting.Indented,
            Converters = new List<JsonConverter>(new JsonConverter[]
            {
                new StringEnumConverter()
            })
        };


        static void Main(string[] args)
        {
            Start().Wait();
            //Start2();

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        static void Start2()
        {
            var container = new SimpleInjector.Container();
            var registrar = new ContainerRegistrar(container);
            var resolver = new ContainerResolver(container);

            RegisterServices(registrar, resolver);

            var context = resolver.Resolve<DbContext>();
            context.Database.Migrate();
            //var user = context
            //    .Set<Entities.User>()
            //    .Where(u => u.Name == "dantte")
            //    .FirstOrDefault();
        }

        static async Task Start()
        {
            var container = new SimpleInjector.Container();
            var registrar = new ContainerRegistrar(container);
            var resolver = new ContainerResolver(container);

            RegisterServices(registrar, resolver);


            using (resolver)
            {
                var context = resolver.Resolve<DbContext>();
                context.Database.Migrate();
                var storeProvider = resolver.Resolve<TypeStoreProvider>();
                var transformer = resolver.Resolve<EntityMapper>();

                #region role
                var query = storeProvider.QueryFor(typeof(Models.Role).FullName);
                var role = query
                    .Query<Entities.Role>(
                        o => o.To(r => r.Users).To(ur => ur.User),
                        o => o.To(r => r.Permissions))
                    .FirstOrDefault(r => r.Name == "Admin")
                    .Pipe(r => transformer.ToModel<Models.Role>(r, MappingIntent.Query));

                if (role == null)
                {
                    role = new Models.Role
                    {
                        Id = Guid.NewGuid(),
                        Name = "Admin",
                        CreatedOn = DateTimeOffset.Now,
                        CreatedBy = Guid.Empty
                    };
                    var command = storeProvider.CommandFor<Models.Role>();
                    await command.Add(role);

                    var json = JsonConvert.SerializeObject(role, SerializerSettings);
                    Console.WriteLine($"Persisted Role: \n{json}");
                }
                else
                {
                    var json = JsonConvert.SerializeObject(role, SerializerSettings);
                    Console.WriteLine($"Retrieved Role: \n{json}");
                }
                #endregion

                #region user
                var user = query
                    .Query<Entities.User>()
                    .FirstOrDefault(u => u.Name == "dantte")
                    .Pipe(u => transformer.ToModel<Models.User>(u, MappingIntent.Query));

                if(user == null)
                {
                    user = new Models.User
                    {
                        Name = "dantte",
                        Id = Guid.NewGuid(),
                        Status = 1
                    };
                    var command = storeProvider.CommandFor<Models.User>();
                    await command.AddToCollection(
                        role,
                        _role => _role.Users,
                        user);

                    var json = JsonConvert.SerializeObject(new[] { user }, SerializerSettings);
                    Console.WriteLine($"Persisted Users: 1\n ");
                }

                else
                {
                    var json = JsonConvert.SerializeObject(new[] { user }, SerializerSettings);
                    Console.WriteLine($"Retrieved Role: \n{json}");
                }
                #endregion
            }
        }


        static void RegisterServices(IServiceRegistrar registrar, IServiceResolver resolver)
        {
            var storeMap = Assembly
                .GetAssembly(typeof(Entities.User))
                .GetExportedTypes()
                .Where(t => t.Extends(typeof(TypeStoreEntry)))
                .Select(Activator.CreateInstance)
                .Cast<TypeStoreEntry>()
                .ToArray()
                .Pipe(entries => new TypeStoreMap(entries));

            registrar.Register(() => resolver, RegistryScope.Singleton);
            registrar.Register(() => storeMap, RegistryScope.Singleton);
            registrar.Register<TypeStoreProvider>(RegistryScope.Singleton);
            registrar.Register<EntityMapper>(RegistryScope.Singleton);
            registrar.Register(() => 
            {
                return new DbContextOptionsBuilder()
                    .UseSqlServer("Data Source=(local); Initial Catalog=JupiterSample; User Id=dev; Password=G3n3r@t10n")
                    .Options;
            }, RegistryScope.Singleton);
            registrar.Register<DbContext>(
                () => new TestContext(resolver.Resolve<DbContextOptions>()), 
                RegistryScope.Singleton);
            registrar.Register<EFStoreCommand>(RegistryScope.Singleton);
            registrar.Register<EFStoreQuery>(RegistryScope.Singleton);
        }

        static string Method1(Func<string> func)
        {
            var result = func();

            //do some manipulation with result...

            return result;
        }
        static string Method2(List<int> values)
        {
            var sb = new System.Text.StringBuilder();
            values.ForEach(value => sb.Append(value));

            return sb.ToString();
        }
        static string Method3()
        {
            var values = new List<int>();

            //populate the list...

            return Method1(() => Method2(values));
        }
    }
}
