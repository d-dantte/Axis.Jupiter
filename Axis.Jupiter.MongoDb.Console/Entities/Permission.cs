using Axis.Jupiter.Configuration;
using Axis.Luna.Extensions;
using System;
using Axis.Jupiter.MongoDb.XModels;
using MongoDB.Driver;
using Axis.Jupiter.Contracts;
using Axis.Jupiter.Helpers;
using Axis.Jupiter.Models;
using System.Collections.Generic;

namespace Axis.Jupiter.MongoDb.ConsoleTest.Entities
{

    public class Permission : BaseEntity<Guid>
    {
        public string Name { get; set; }

        public string Scope { get; set; }

        public EntityRef<Role, Guid> Role { get; set; }

        public Permission()
        {
        }
    }

    public class PermissionConfig: EntityInfo<Permission, Guid>, ITypeMapper
    {
        public static TypeStoreEntry StoreEntry() => new TypeStoreEntry(
            typeName: typeof(Models.Permission).FullName,
            mapper: Singleton,
            commandServiceType: typeof(XMongoStoreCommand));

        public static PermissionConfig Singleton { get; } = new PermissionConfig();


        #region ITypeMapper
        public Type ModelType => typeof(Models.Permission);

        public object NewEntity(object model) => new Entities.Permission();

        public object NewModel(object entity) => new Models.Permission();

        public IEnumerable<CollectionRefInfo> ToCollectionRefInfos<TModel>(
            object parentModel,
            MappingIntent intent,
            string propertyName,
            TModel[] children,
            MappingContext context)
        where TModel : class => null;

        public object ToEntity(object model, object entity, MappingIntent intent, MappingContext context)
        {
            var permModel = (Models.Permission)model;
            var permEntity = permModel.CopyBase((Entities.Permission)entity);

            var role = context.EntityMapper
                .ToEntity<Models.Role>(
                    permModel.Role,
                    intent,
                    context)
                .As<Entities.Role>();

            if(role != null)
                permEntity.Role = Provider.CreateRef<Entities.Role, Guid>(role);

            permEntity.Name = permModel.Name;
            permEntity.Scope = permModel.Scope;

            return permModel;
        }

        public object ToModel(object entity, object model, MappingIntent intent, MappingContext context)
        {
            var permEntity = (Entities.Permission)entity;
            var permModel = permEntity.CopyBase((Models.Permission)model);

            permModel.Role = context.EntityMapper.ToModel<Models.Role>(
                permEntity.Role?.RefInstance,
                intent,
                context);

            permModel.Name = permEntity.Name;
            permModel.Scope = permEntity.Scope;

            return permModel;
        }
        #endregion

        #region EntityInfo
        public override string Database => "Elara-Test";

        public override MongoDatabaseSettings DatabaseSettings => new MongoDatabaseSettings
        {
            GuidRepresentation = MongoDB.Bson.GuidRepresentation.CSharpLegacy
        };

        #endregion

        private PermissionConfig()
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
