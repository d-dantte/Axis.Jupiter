using Axis.Jupiter.Configuration;
using Axis.Jupiter.Contracts;
using Axis.Jupiter.Helpers;
using Axis.Jupiter.Models;
using Axis.Luna.Extensions;
using System;

namespace Axis.Jupiter.EFCore.ConsoleTest.Entities
{
    using System.Collections.Generic;
    using CollectionRefFunc = Func<object, string, MappingIntent, MappingContext, System.Collections.Generic.IEnumerable<CollectionRefInfo>>;
    using TransformFunc = System.Func<object, object, MappingIntent, MappingContext, object>;


    public class Permission : BaseEntity<Guid>
    {
        public string Name { get; set; }
        public string Scope { get; set; }

        public virtual Role Role { get; set; }
        public Guid RoleId { get; set; }
    }

    public class PermissionStoreEntry : TypeStoreEntry
    {
        public PermissionStoreEntry() : base(
            typeof(Models.Permission).FullName,
            typeof(EFStoreQuery),
            typeof(EFStoreCommand),
            new PermissionEntityTransform())
        {
        }
    }


    public class PermissionEntityTransform : ITypeMapper
    {
        public Type ModelType => typeof(Models.Permission);

        public Type EntityType => typeof(Entities.Permission);

        public object NewEntity(object model) => new Permission();

        public object NewModel(object entity) => new Models.Permission();

        public object ToModel(
            object entity,
            object model,
            MappingIntent intent,
            MappingContext context)
        {
            var permEntity = (Entities.Permission)entity;
            var permModel = permEntity.CopyTo((Models.Permission)model);

            if (intent == MappingIntent.Query) permModel.IsPersisted = true;
            permModel.Role = context.EntityMapper.ToModel<Models.Role>(
                permEntity.Role,
                intent,
                context);

            permModel.Name = permEntity.Name;
            permModel.Scope = permEntity.Scope;

            return permModel;
        }

        public object ToEntity(
            object model,
            object entity,
            MappingIntent intent,
            MappingContext context)
        {
            var permModel = (Models.Permission)model;
            var permEntity = permModel.CopyTo((Entities.Permission)entity);

            permEntity.Role = context.EntityMapper
                .ToEntity<Models.Role>(
                    permModel.Role,
                    intent,
                    context)
                .As<Entities.Role>();

            permEntity.RoleId = permEntity.Role?.Id ?? Guid.Empty;
            permEntity.Name = permModel.Name;
            permEntity.Scope = permModel.Scope;

            return permModel;
        }

        public IEnumerable<CollectionRefInfo> ToCollectionRefInfos<TModel>(
            object parentModel,
            MappingIntent intent,
            string propertyName,
            TModel[] children,
            MappingContext context)
            where TModel : class
        {
            throw new Exception("Invalid property: " + propertyName);
        }
    }
}
