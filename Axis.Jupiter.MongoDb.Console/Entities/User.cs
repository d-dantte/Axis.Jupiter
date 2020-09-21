using Axis.Jupiter.Models;
using Axis.Luna.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using Axis.Jupiter.MongoDb.Models;
using MongoDB.Driver;
using Axis.Jupiter.Contracts;
using Axis.Jupiter.Helpers;
using Axis.Jupiter.MongoDb.XModels;
using Axis.Jupiter.Configuration;

namespace Axis.Jupiter.MongoDb.ConsoleTest.Entities
{
    public class User: BaseEntity<Guid>
    {
        public BioData Bio { get; set; }

        public int Status { get; set; }

        public string Name { get; set; }

        public EntitySetRef<Entities.Role, Guid, Guid> Roles { get; set; }


        public User()
        {
        }
    }


    public class UserConfig : EntityInfo<Entities.User, Guid>, ITypeMapper
    {
        public Type ModelType => typeof(Models.User);

        public static TypeStoreEntry StoreEntry() => new TypeStoreEntry(
            typeName: typeof(Models.User).FullName,
            mapper: Singleton,
            commandServiceType: typeof(XMongoStoreCommand));

        public static UserConfig Singleton { get; } = new UserConfig();


        public object NewEntity(object model) => new Entities.User();

        public object NewModel(object entity) => new Models.User();

        public object ToModel(object entity, object model, MappingIntent intent, MappingContext context)
        {
            var userEntity = (Entities.User)entity;
            var userModel = userEntity.CopyBase((Models.User)model);

            userModel.Name = userEntity.Name;
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

            userEntity.Roles?.Refs
                .Where(@ref => @ref.RefInstance != null)
                .Select(@ref => context.EntityMapper
                .ToModel<Models.Role>(@ref.RefInstance, intent, context))
                .Pipe(userModel.Roles.AddRange);

            return userModel;
        }

        public object ToEntity(object model, object entity, MappingIntent intent, MappingContext context)
        {
            var userModel = (Models.User)model;
            var userEntity = userModel.CopyBase((Entities.User)entity);

            userEntity.Name = userModel.Name;
            userEntity.Status = userModel.Status;
            userEntity.Bio = new BioData
            {
                DateOfBirth = userModel.Bio.DateOfBirth,
                FirstName = userModel.Bio.FirstName,
                LastName = userModel.Bio.LastName,
                MiddleName = userModel.Bio.MiddleName,
                Nationality = userModel.Bio.Nationality,
                Sex = userModel.Bio.Sex
            };
            

            userEntity.Roles = Provider.CreateSetRef<Entities.Role, Guid, Guid>(
                userEntity.Key,
                nameof(Entities.User.Roles),
                userModel.Roles
                    .Where(r => !r.IsPersisted) //leave only non-persisted roles there so they can get persisted too
                    .Select(r => context.EntityMapper.ToEntity(r, intent, context))
                    .Cast<Entities.Role>()
                    .ToArray());

            return userEntity;
        }

        public IEnumerable<CollectionRefInfo> ToCollectionRefInfos<TModel>(
            object parent,
            MappingIntent intent,
            string property,
            TModel[] children,
            MappingContext context)
            where TModel : class
        {
            var userModel = parent.As<Models.User>();
            switch (property)
            {
                case nameof(Models.User.Roles):
                    return children.Cast<Models.Role>()
                        .Select(child => new CollectionRefInfo(
                            model: child,
                            result: RefInfoResult.Model,
                            command: intent == MappingIntent.Add ? CollectionRefCommand.Add : CollectionRefCommand.Remove,
                            entity: Provider
                                .CreateSetRefEntity<Entities.Role, Guid, Guid>(
                                    userModel.Id,
                                    child.Id,
                                    nameof(Entities.User.Roles))))
                        .ToArray();

                default:
                    throw new ArgumentException($"Invalid Property: {property}");
            }
        }

        private bool IsNotNull(object obj) => obj != null;

        private UserConfig()
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
