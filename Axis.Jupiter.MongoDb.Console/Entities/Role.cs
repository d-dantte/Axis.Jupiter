using Axis.Jupiter.Configuration;
using Axis.Jupiter.Contracts;
using Axis.Jupiter.Helpers;
using Axis.Jupiter.Models;
using Axis.Luna.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using Axis.Jupiter.MongoDb.XModels;
using MongoDB.Driver;

namespace Axis.Jupiter.MongoDb.ConsoleTest.Entities
{

    public class Role: BaseEntity<Guid>
    {
        public string Name { get; set; }

        public EntitySetRef<Permission, Guid, Guid> Permissions { get; set; }

        public EntitySetRef<User, Guid, Guid> Users { get; set; }

        public Role()
        {
        }
    }


    public class RoleConfig : EntityInfo<Role, Guid>, ITypeMapper
    {
        public Type ModelType => typeof(Models.Role);

        public static TypeStoreEntry StoreEntry() => new TypeStoreEntry(
            typeName: typeof(Models.Role).FullName,
            mapper: Singleton,
            commandServiceType: typeof(XMongoStoreCommand));

        public static RoleConfig Singleton { get; } = new RoleConfig();


        public object NewEntity(object model) => new Entities.Role();

        public object NewModel(object entity) => new Models.Role();

        public object ToModel(object entity, object model, MappingIntent intent, MappingContext context)
        {
            var roleEntity = (Entities.Role)entity;
            var roleModel = roleEntity.CopyBase((Models.Role)model);

            roleModel.Name = roleEntity.Name;

            roleEntity.Permissions.Refs
                .Select(permission =>
                {
                    if (permission.RefInstance == null)
                        throw new Exception("Invalid Permission Ref");

                    return context.EntityMapper
                        .ToModel<Models.Permission>(permission, intent, context);
                })
                .Pipe(roleModel.Permissions.AddRange);

            roleEntity.Users.Refs
                .Select(user =>
                {
                    if (user.RefInstance == null)
                        throw new Exception("Invalid User Ref");

                    return context.EntityMapper
                        .ToModel<Models.User>(user, intent, context);
                })
                .Pipe(roleModel.Users.AddRange);

            return roleModel;
        }

        public object ToEntity(object model, object entity, MappingIntent intent, MappingContext context)
        {
            var roleModel = (Models.Role)model;
            var roleEntity = roleModel.CopyBase((Entities.Role)entity);

            roleEntity.Name = roleModel.Name;

            var permissions = roleModel.Permissions
                 .Where(IsNotNull)
                 .Select(permission => context.EntityMapper
                 .ToEntity(permission, intent, context))
                 .Cast<Entities.Permission>()
                 .FilterForIntent(intent)
                 .ToArray();

            roleEntity.Permissions = Provider.CreateSetRef<Entities.Permission, Guid, Guid>(
                roleEntity.Key,
                nameof(Entities.Role.Permissions),
                permissions);


            var users = roleModel.Users
                 .Where(IsNotNull)
                 .Select(user => context.EntityMapper
                 .ToEntity(user, intent, context))
                 .Cast<Entities.User>()
                 .FilterForIntent(intent)
                 .ToArray();

            roleEntity.Users = Provider.CreateSetRef<Entities.User, Guid, Guid>(
                roleEntity.Key,
                nameof(Entities.Role.Users),
                users);

            return roleEntity;
        }

        public IEnumerable<CollectionRefInfo> ToCollectionRefInfos<TModel>(
            object parent, 
            MappingIntent intent, 
            string property, 
            TModel[] children, 
            MappingContext context) 
            where TModel : class
        {
            var roleModel = parent.As<Models.Role>();
            switch (property)
            {
                case nameof(Models.Role.Permissions):
                    return children.Cast<Models.Permission>().Select(child => new CollectionRefInfo(
                        model: child,
                        result: RefInfoResult.Model,
                        command: intent == MappingIntent.Add ? CollectionRefCommand.Add : CollectionRefCommand.Remove,
                        entity: Provider.CreateSetRefEntity<Entities.Role, Guid, Guid>(
                            roleModel.Id,
                            child.Id,
                            nameof(Entities.Role.Permissions))))
                    .ToArray();

                case nameof(Models.Role.Users):
                    return children.Cast<Models.User>().Select(child => new CollectionRefInfo(
                        model: child,
                        result: RefInfoResult.Model,
                        command: intent == MappingIntent.Add ? CollectionRefCommand.Add : CollectionRefCommand.Remove,
                        entity: Provider.CreateSetRefEntity<Entities.Role, Guid, Guid>(
                            roleModel.Id,
                            child.Id,
                            nameof(Entities.Role.Users))))
                    .ToArray();

                default:
                    throw new ArgumentException($"Invalid Property: {property}");
            }
        }

        private bool IsNotNull(object obj) => obj != null;

        private RoleConfig()
        {
            Database = "Elara-Test";

            QueryOptions = new AggregateOptions
            {

            };

            DeleteOptions = new DeleteOptions
            {

            };

            InsertSingleOptions = new InsertOneOptions
            {

            };

            InsertMultipleOptions = new InsertManyOptions
            {

            };

            UpdateOptions = new UpdateOptions
            {

            };
        }
    }
}
