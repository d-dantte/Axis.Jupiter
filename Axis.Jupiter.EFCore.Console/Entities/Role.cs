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
    public class Role: BaseEntity<Guid>
    {
        public string Name { get; set; }

        public virtual ICollection<Permission> Permissions { get; set; } = new HashSet<Permission>();
        public virtual ICollection<UserRole> Users { get; set; } = new HashSet<UserRole>();
    }


    public class RoleStoreEntry : TypeStoreEntry
    {
        public RoleStoreEntry() : base(
            typeof(Models.Role).FullName,
            typeof(EFStoreQuery),
            typeof(EFStoreCommand),
            new RoleEntityTransform())
        {
        }
    }

    public class RoleEntityTransform : ITypeMapper
    {
        public Type ModelType => typeof(Models.Role);

        public Type EntityType => typeof(Entities.Role);

        public object NewEntity(object model) => new Role();

        public object NewModel(object entity) => new Models.Role();

        public object ToModel(
            object entity,
            object model,
            MappingIntent intent,
            MappingContext context)
        {
            var roleEntity = (Entities.Role)entity;
            var roleModel = roleEntity.CopyTo((Models.Role)model);

            if(intent == MappingIntent.Query) roleModel.IsPersisted = true;
            roleModel.Name = roleEntity.Name;

            roleEntity.Permissions
                .Select(permission => context.EntityMapper
                .ToModel<Models.Permission>(
                    permission,
                    intent,
                    context))
                .Pipe(roleModel.Permissions.AddRange);

            roleEntity.Users
                .Select(ur => context.EntityMapper
                .ToModel<Models.User>(
                    ur.User,
                    intent,
                    context))
                .Pipe(roleModel.Users.AddRange);

            return roleModel;
        }

        public object ToEntity(
            object model,
            object entity,
            MappingIntent intent,
            MappingContext context)
        {
            var roleModel = (Models.Role)model;
            var roleEntity = roleModel.CopyTo((Entities.Role)entity);            

            roleEntity.Name = roleModel.Name;

            roleModel.Permissions
                .Select(permission => context.EntityMapper
                .ToEntity<Models.Permission>(
                    permission,
                    intent,
                    context))
                .Cast<Entities.Permission>()
                .Pipe(roleEntity.Permissions.AddRange);

            roleModel.Users
                .Select(user => new Entities.UserRole
                {
                    UserId = user.Id,
                    RoleId = roleModel.Id,
                })
                .Pipe(roleEntity.Users.AddRange);

            return roleEntity;
        }

        public IEnumerable<CollectionRefInfo> ToCollectionRefInfos<TModel>(
            object parentModel,
            MappingIntent intent,
            string propertyName,
            TModel[] children,
            MappingContext context)
            where TModel : class
        {
            var roleModel = parentModel.As<Models.Role>();
            switch(propertyName)
            {
                case nameof(Models.Role.Permissions):
                    return children
                        .Cast<Models.Permission>()
                        .Select(permission =>
                        {
                            var pentity = context.EntityMapper.ToEntity(
                                model: permission,
                                context: context,
                                command: intent == MappingIntent.Remove ? MappingIntent.Remove : MappingIntent.Update)
                                .As<Entities.Permission>();

                            var rentity = context.EntityMapper.ToEntity(
                                model: roleModel,
                                context: context,
                                command: MappingIntent.Query)
                                .As<Entities.Role>();

                            pentity.Role = null;
                            pentity.RoleId = Guid.Empty;

                            if (intent != MappingIntent.Remove)
                                pentity.RoleId = rentity.Id;
                            
                            return new CollectionRefInfo(
                                result: RefInfoResult.Entity,
                                model: permission,
                                entity: pentity,
                                command: CollectionRefCommand.Update);
                        });

                case nameof(Models.Role.Users):
                    switch (intent)
                    {
                        case MappingIntent.Add:
                        case MappingIntent.Remove:
                            return children
                                .Cast<Models.User>()
                                .SelectMany(user =>
                                {
                                    var userEntity = context.EntityMapper.ToEntity(
                                        user,
                                        MappingIntent.Add,
                                        context)
                                        .As<User>();

                                    var refinfos = new List<CollectionRefInfo>();

                                    if (!user.IsPersisted)
                                        refinfos.Add(new CollectionRefInfo(
                                            command: CollectionRefCommand.Add,
                                            model: user,
                                            entity: userEntity,
                                            rank: 1));

                                    refinfos.Add(new CollectionRefInfo(
                                        command: intent == MappingIntent.Remove ? CollectionRefCommand.Remove : CollectionRefCommand.Add,
                                        result: RefInfoResult.Model,
                                        rank: 0,
                                        model: user,
                                        entity: new UserRole
                                        {
                                            User = userEntity,
                                            RoleId = roleModel.Id
                                        }));

                                    return refinfos;
                                });

                        default: throw new Exception("Invalid Mapping Intent");
                    }

                default:
                    throw new ArgumentException($"Invalid Property: {propertyName}");
            }
        }
    }
}
