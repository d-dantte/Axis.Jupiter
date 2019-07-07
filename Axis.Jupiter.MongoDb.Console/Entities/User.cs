using Axis.Jupiter.Models;
using Axis.Luna.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using Axis.Jupiter.MongoDb.Models;
using MongoDB.Driver;
using Axis.Jupiter.Contracts;
using Axis.Jupiter.Helpers;

namespace Axis.Jupiter.MongoDb.ConsoleTest.Entities
{
    public class User: BaseEntity<Guid>
    {
        public BioData Bio { get; set; }

        public int Status { get; set; }

        public string Name { get; set; }

        public SecondaryRef<Guid, Guid, Role>[] RoleRefs { get; set; } = new SecondaryRef<Guid, Guid, Role>[0];

        ///<inheritdoc/>
        public override IEnumerable<IEntityCollectionRef> EntityCollectionRefs() => new IEntityCollectionRef[0];

        /// <inheritdoc/>
        public override IEnumerable<IEntityRef> EntityRefs() => RoleRefs?.ToArray() ?? new IEntityRef[0];


        public User()
        {
        }
    }

    public class UserEntityInfo : EntityInfo<User, Guid>
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

    public class UserEntityMapper : ITypeMapper
    {
        public Type ModelType => typeof(Models.User);

        public Type EntityType => typeof(Entities.User);

        public object NewEntity(object model) => new Entities.User();

        public object NewModel(object entity) => new Models.User();

        public object ToEntity(object model, object entity, MappingIntent intent, MappingContext context)
        {
            var userModel = (Models.User)model;
            var userEntity = userModel.CopyBase((Entities.User)entity);

            userEntity.Bio = userModel.Bio == null ? null : new Entities.BioData
            {
                DateOfBirth = userModel.Bio.DateOfBirth,
                FirstName = userModel.Bio.FirstName,
                LastName = userModel.Bio.LastName,
                MiddleName = userModel.Bio.MiddleName,
                Nationality = userModel.Bio.Nationality,
                Owner = userEntity,
                Sex = userModel.Bio.Sex
            };

            userEntity.Name = userModel.Name;

            userEntity.RoleRefs = userModel.Roles
                .Select(role => context.EntityMapper
                .ToEntity<Models.Role>(role, intent, context))
                .Cast<Entities.Role>()
                .Select(role => new SecondaryRef<Guid, Guid, Role>(userEntity, role))
                .ToArray();

            userEntity.Status = userModel.Status;

            return userEntity;
        }

        public object ToModel(object entity, object model, MappingIntent intent, MappingContext context)
        {
            var userEntity = (Entities.User)entity;
            var userModel = userEntity.CopyBase((Models.User)model);

            userModel.Name = userEntity.Name;

            userEntity.RoleRefs
                .Where(@ref => @ref.Entity != null)
                .Select(@ref => context.EntityMapper
                .ToModel<Models.Role>(@ref.Entity, intent, context))
                .Pipe(userModel.Roles.AddRange);

            userModel.Status = userEntity.Status;

            userModel.Bio = userEntity.Bio == null ? null : new Models.BioData
            {
                DateOfBirth = userEntity.Bio.DateOfBirth,
                FirstName = userEntity.Bio.FirstName,
                LastName = userEntity.Bio.LastName,
                MiddleName = userEntity.Bio.MiddleName,
                Nationality = userEntity.Bio.Nationality,
                Sex = userEntity.Bio.Sex,
                Owner = userModel
            };

            return userModel;
        }

        public IEnumerable<CollectionRefInfo> ToCollectionRefInfos<TModel>(
            object parentModel,
            MappingIntent intent,
            string propertyName,
            TModel[] children,
            MappingContext context)
            where TModel : class
        {
            if (propertyName != nameof(Models.User.Roles))
                throw new ArgumentException($"Invalid Property: {propertyName ?? "null"}");

            var user = parentModel as Models.User;

            switch (intent)
            {
                case MappingIntent.Add:
                case MappingIntent.Remove:
                    return children
                        .Cast<Models.Role>()
                        .Select(role =>
                        {
                            var roleEntity = context.EntityMapper.ToEntity(
                                role,
                                MappingIntent.Add,
                                context)
                                .As<Role>();

                            var refinfos = new List<CollectionRefInfo>();

                            if (!role.IsPersisted)
                                refinfos.Add(new CollectionRefInfo(
                                    command: CollectionRefCommand.Add,
                                    model: role,
                                    entity: roleEntity,
                                    rank: 1));

                            return new CollectionRefInfo(
                                result: RefInfoResult.Model,
                                rank: 0,
                                command: intent == MappingIntent.Remove ? CollectionRefCommand.Remove : CollectionRefCommand.Add,
                                model: role,
                                entity: new UserRole
                                {
                                    UserId = user.Id,
                                    Role = roleEntity
                                });
                        });

                default: throw new Exception("Invalid Mapping Intent");
            }
        }
    }

}
