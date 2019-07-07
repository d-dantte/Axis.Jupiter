using Axis.Jupiter.Configuration;
using Axis.Luna.Extensions;
using System;
using System.Collections.Generic;
using Axis.Jupiter.MongoDb.Models;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace Axis.Jupiter.MongoDb.ConsoleTest.Entities
{

    public class Permission : BaseEntity<Guid>
    {
        public string Name { get; set; }

        public string Scope { get; set; }

        [BsonIgnore]
        public virtual Role Role => RoleRef.Entity;

        public SecondaryRef<Guid, Guid, Role> RoleRef { get; }

        public Guid RoleId { get; set; }

        public override IEnumerable<IEntityCollectionRef> EntityCollectionRefs()
        => new IEntityCollectionRef[0];

        public override IEnumerable<IEntityRef> EntityRefs()
        => new IEntityRef[] { RoleRef };

        public Permission()
        {
            RoleRef = new SecondaryRef<Guid, Guid, Role>(this);
        }

        public Permission(SecondaryRef<Guid, Guid, Role> roleRef)
        {
            RoleRef = roleRef ?? throw new ArgumentException(nameof(roleRef));
            RoleRef.Referrer = this;
        }
    }

    public class PermissionStoreEntry : TypeStoreEntry
    {
        public PermissionStoreEntry() : base(
            typeof(Models.Permission).FullName,
            typeof(MongoStoreCommand),
            NewTransformInstance())
        {
        }

        private static DefaultTypeTransform<Entities.Permission, Models.Permission> NewTransformInstance()
        => new DefaultTypeTransform<Entities.Permission, Models.Permission>
        {
            NewEntity = (model) => new Permission(),
            NewModel = (entity) => new Models.Permission(),

            ToModel = (entity, model, command, context) =>
            {
                var permEntity = (Entities.Permission)entity;
                var permModel = permEntity.CopyBase((Models.Permission)model);

                permModel.Role = context.Transformer.ToModel<Models.Role>(
                        permEntity.Role,
                        command,
                        context);

                permModel.Name = permEntity.Name;
                permModel.Scope = permEntity.Scope;

                return permModel;
            },

            ToEntity = (model, entity, command, context) =>
            {
                var permModel = (Models.Permission)model;
                var permEntity = permModel.CopyBase((Entities.Permission)entity);

                permEntity.RoleRef.Entity = context.Transformer
                        .ToEntity<Models.Role>(
                            permModel.Role,
                            command,
                            context)
                        .As<Entities.Role>();

                permEntity.RoleId = permEntity.Role?.Key ?? Guid.Empty;
                permEntity.Name = permModel.Name;
                permEntity.Scope = permModel.Scope;

                return permModel;
            },

            ToEntityCollectionRef = null
        };
    }

    public class PermissionEntityInfo : EntityInfo<Permission, Guid>
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
