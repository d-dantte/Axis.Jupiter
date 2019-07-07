using Axis.Jupiter.Configuration;
using Axis.Jupiter.Contracts;
using Axis.Jupiter.Helpers;
using Axis.Jupiter.Models;
using Axis.Luna.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Axis.Jupiter.EFCore.ConsoleTest.Entities
{
    public class User: BaseEntity<Guid>
    {
        public virtual BioData Bio { get; set; }

        public int Status { get; set; }
        public string Name { get; set; }

        public virtual ICollection<UserRole> Roles { get; set; } = new HashSet<UserRole>();
    }


    public class UserStoreEntry: TypeStoreEntry
    {
        public UserStoreEntry(): base(
            typeof(Models.User).FullName,
            typeof(EFStoreQuery),
            typeof(EFStoreCommand),
            new UserEntityTransform())
        {
        }
    }

    public class UserEntityTransform : ITypeMapper
    {
        public Type ModelType => typeof(Models.User);

        public Type EntityType => typeof(Entities.User);

        public object NewEntity(object model) => new User();

        public object NewModel(object entity) => new Models.User();

        public object ToModel(
            object entity,
            object model,
            MappingIntent intent,
            MappingContext context)
        {
            var userEntity = (Entities.User)entity;
            var userModel = userEntity.CopyTo((Models.User)model);

            if(intent == MappingIntent.Query) userModel.IsPersisted = true;
            userModel.Bio = context.EntityMapper.ToModel<Models.BioData>(
                userEntity.Bio,
                intent,
                context);

            userModel.Name = userEntity.Name;

            userEntity.Roles
                .Select(userrole => context.EntityMapper
                .ToModel<Models.Role>(
                    userrole.Role,
                    intent,
                    context))
                .Pipe(userModel.Roles.AddRange);

            userModel.Status = userEntity.Status;

            return userModel;
        }

        public object ToEntity(
            object model,
            object entity,
            MappingIntent intent,
            MappingContext context)
        {
            var userModel = (Models.User)model;
            var userEntity = userModel.CopyTo((Entities.User)entity);

            userEntity.Bio = context.EntityMapper
                .ToEntity<Models.BioData>(
                    userModel.Bio,
                    intent,
                    context)
                .As<Entities.BioData>();

            userEntity.Name = userModel.Name;

            userModel.Roles
                .Select(role => new Entities.UserRole
                {
                    RoleId = role.Id,
                    UserId = userModel.Id,
                })
                .Pipe(userEntity.Roles.AddRange);

            userEntity.Status = userModel.Status;

            return userEntity;
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

            switch(intent)
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
