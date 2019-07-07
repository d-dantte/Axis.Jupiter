using Axis.Jupiter.Configuration;
using Axis.Jupiter.Contracts;
using Axis.Jupiter.Helpers;
using Axis.Jupiter.Models;
using Axis.Luna.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using Axis.Jupiter.MongoDb.Models;
using MongoDB.Driver;

namespace Axis.Jupiter.MongoDb.ConsoleTest.Entities
{


    public class Role: BaseEntity<Guid>
    {
        public string Name { get; set; }

        public EntityCollectionRef<Guid, Permission> PermissionRefs { get; }

        public SecondaryRef<Guid, Guid, User>[] UserRefs { get; set; } = new SecondaryRef<Guid, Guid, User>[0];

        public override IEnumerable<IEntityCollectionRef> EntityCollectionRefs()
        => new IEntityCollectionRef[]
        {
            PermissionRefs
        };

        public override IEnumerable<IEntityRef> EntityRefs() => UserRefs.ToArray();

        public Role()
        {
            PermissionRefs = new EntityCollectionRef<Guid, Permission>(this);
        }

        public Role(EntityCollectionRef<Guid, Permission> permissionRefs)
        {
            PermissionRefs = permissionRefs ?? throw new ArgumentException(nameof(permissionRefs));
            PermissionRefs.Referrer = this;
        }
    }


    public class RoleStoreEntry : TypeStoreEntry
    {
        public RoleStoreEntry() : base(
            typeof(Models.Role).FullName,
            typeof(MongoStoreCommand),
            NewTransformInstance())
        {
        }

        private static DefaultTypeTransform<Entities.Role, Models.Role> NewTransformInstance()
        => new DefaultTypeTransform<Role, Models.Role>
        {
            NewEntity = (model) => new Role(),

            NewModel = (entity) => new Models.Role(),

            ToModel = (entity, model, command, context) =>
            {
                var roleEntity = (Entities.Role)entity;
                var roleModel = roleEntity.CopyBase((Models.Role)model);

                roleModel.Name = roleEntity.Name;

                roleEntity.PermissionRefs.EntityCollection
                    .Select(permission => context.Transformer
                    .ToModel<Models.Permission>(permission, command, context))
                    .Pipe(roleModel.Permissions.AddRange);

                roleEntity.UserRefs
                    .Where(user => user.Entity != null)
                    .Select(user => context.Transformer
                    .ToModel<Models.User>(user.Entity, command, context))
                    .Pipe(roleModel.Users.AddRange);

                return roleModel;
            },

            ToEntity = (model, entity, command, context) =>
            {
                var roleModel = (Models.Role)model;
                var roleEntity = roleModel.CopyBase((Entities.Role)entity);

                roleEntity.Name = roleModel.Name;

                roleModel.Permissions
                    .Select(permission => context.Transformer
                    .ToEntity<Models.Permission>(permission, command, context))
                    .Cast<Entities.Permission>()
                    .ForAll(permission => roleEntity.PermissionRefs.EntityCollection.Add(permission));

                roleEntity.UserRefs = roleModel.Users
                    .Select(user => context.Transformer
                    .ToEntity<Models.User>(user, command, context))
                    .Cast<Entities.User>()
                    .Select(user => new SecondaryRef<Guid, Guid, User>(roleEntity, user))
                    .ToArray();

                return roleEntity;
            },

            ToEntityCollectionRef = (parent, property, child, command, context) =>
            {
                var roleModel = parent.As<Models.Role>();
                switch (property)
                {
                    case nameof(Models.Role.Permissions):
                        var permissionEntity = context.Transformer
                            .ToEntity(
                                child.As<Models.Permission>(),
                                command,
                                context)
                            .As<Entities.Permission>();

                        permissionEntity.RoleId = roleModel.Id;
                        return new EntityRef
                        {
                            Name = property,
                            RefRelativity = EntityRefRelativity.Destination,
                            RefType = EntityRefType.OneToMany,
                            Entity = permissionEntity
                        };

                    default:
                        throw new ArgumentException($"Invalid Property: {property}");
                }
            }
        };
    }


    public class RoleEntityInfo : EntityInfo<Role, Guid>
    { 
        public override string Database => "Elara-Test";

        public override MongoDatabaseSettings DatabaseSettings => new MongoDatabaseSettings
        {
            GuidRepresentation = MongoDB.Bson.GuidRepresentation.CSharpLegacy
        };

        public override AggregateOptions QueryOptions => new AggregateOptions
        {

        };

        public override DeleteOptions DeleteOptions => new DeleteOptions
        {

        };

        public override InsertOneOptions InsertSingleOptions => new InsertOneOptions
        {

        };

        public override InsertManyOptions InsertMultipleOptions => new InsertManyOptions
        {

        };

        public override UpdateOptions UpdateOptions => new UpdateOptions
        {

        };
    }
}
