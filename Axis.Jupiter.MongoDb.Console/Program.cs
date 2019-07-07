using Axis.Jupiter.Configuration;
using Axis.Jupiter.Helpers;
using Axis.Jupiter.Providers;
using Axis.Luna.Extensions;
using Axis.Proteus.Ioc;
using Axis.Proteus.SimpleInjector;
using MongoDB.Driver;
using System;
using System.Linq;
using System.Reflection;

namespace Axis.Jupiter.MongoDb.ConsoleTest
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Start2();
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }

            System.Console.Write("Hit any key to exit...");
            System.Console.ReadKey();
        }

        static void Start2()
        {
            var container = new SimpleInjector.Container();
            var registrar = new ContainerRegistrar(container);
            var resolver = new ContainerResolver(container);

            RegisterServices(registrar, resolver);

            var client = resolver.Resolve<MongoDB.Driver.MongoClient>();
            var provider = resolver.Resolve<TypeStoreProvider>();
            var infoProvider = resolver.Resolve<EntityInfoProvider>();
            var transformer = resolver.Resolve<EntityMapper>();


            var tyrant = new Models.Role
            {
                Name = "#tyrant",
                Id = Guid.Parse("2bd3392e-192a-4a3a-9e3a-7a492a480929")
            };

            #region User
            var userCommand = provider.CommandFor<Models.User>();
            var userInfo = infoProvider.InfoFor<Entities.User>();

            var polyMaster = client
                .GetDatabase(userInfo.Database)
                .GetCollection<Entities.User>(userInfo.CollectionName)
                .AsQueryable()
                .FirstOrDefault(_u => _u.Name == "@polymaster")
                .Pipe(_u => transformer.ToModel<Models.User>(_u, MappingIntent.Query));

            if (polyMaster == null)
            {
                polyMaster = new Models.User
                {
                    Id = Guid.NewGuid(),
                    Name = "@polymaster",
                    Status = 0,
                    Bio = new Models.BioData
                    {
                        DateOfBirth = DateTime.Now - TimeSpan.FromDays(12 * 365),
                        FirstName = "Homer",
                        LastName = "Simpson",
                        MiddleName = "Albert",
                        Nationality = "Bleh",
                        Sex = Models.Sex.Male
                    }
                };

                polyMaster.Bio.Owner = polyMaster;
                polyMaster.Roles.Add(tyrant);

                userCommand
                    .Add(polyMaster)
                    .Resolve();
            }

            //update

            polyMaster.Bio.FirstName = "Homer " + DateTime.Now.Ticks;

            userCommand
                .Update(polyMaster)
                .Resolve();

            #endregion

            #region Role
            var roleCommand = provider.CommandFor<Models.Role>();
            var roleInfo = infoProvider.InfoFor<Entities.Role>();

            var tyrantRole = client
                .GetDatabase(roleInfo.Database)
                .GetCollection<Entities.Role>(roleInfo.CollectionName)
                .AsQueryable()
                .FirstOrDefault(_r=> _r.Name == "#tyrant")
                .Pipe(_u => transformer.ToModel<Models.Role>(_u, MappingIntent.Query));
            #endregion
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

            var infoProvider = Assembly
                .GetAssembly(typeof(Entities.User))
                .GetExportedTypes()
                .Where(t => t.Implements(typeof(IEntityInfo)))
                .Select(Activator.CreateInstance)
                .Cast<IEntityInfo>()
                .Pipe(infoList => new EntityInfoProvider(infoList));

            registrar.Register(() => resolver, RegistryScope.Singleton);
            registrar.Register(() => storeMap, RegistryScope.Singleton);
            registrar.Register(() => infoProvider, RegistryScope.Singleton);
            registrar.Register<TypeStoreProvider>(RegistryScope.Singleton);
            registrar.Register<EntityMapper>(RegistryScope.Singleton);
            registrar.Register(() =>
            {
                return new MongoDB.Driver.MongoClient(new MongoDB.Driver.MongoClientSettings
                {
                    Server = new MongoDB.Driver.MongoServerAddress("localhost", 27017)
                });
            }, RegistryScope.Singleton);
            registrar.Register<MongoStoreCommand>(RegistryScope.Singleton);
        }
    }
}
