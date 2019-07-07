using Axis.Jupiter.Configuration;
using Axis.Jupiter.Contracts;
using Axis.Jupiter.Helpers;
using Axis.Jupiter.Models;
using Axis.Luna.Extensions;
using System;
using System.Collections.Generic;

namespace Axis.Jupiter.EFCore.ConsoleTest.Entities
{

    public class BioData: BaseEntity<Guid>
    {
        public virtual User Owner { get; set; }
        public Guid OwnerId { get; set; }

        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }

        public Models.Sex Sex { get; set; }

        public DateTimeOffset DateOfBirth { get; set; }
        public string Nationality { get; set; }
    }

    public class BioDataStoreEntry : TypeStoreEntry
    {
        public BioDataStoreEntry() : base(
            typeof(Models.BioData).FullName,
            typeof(EFStoreQuery),
            typeof(EFStoreCommand),
            new BioDataEntityTransform())
        {
        }
    }


    public class BioDataEntityTransform : ITypeMapper
    {
        public Type ModelType => typeof(Models.BioData);

        public Type EntityType => typeof(Entities.BioData);

        public object NewEntity(object model) => new BioData();

        public object NewModel(object entity) => new Models.BioData();

        public object ToModel(
            object entity,
            object model,
            MappingIntent intent,
            MappingContext context)
        {
            var bioEntity = (Entities.BioData)entity;
            var bioModel = bioEntity.CopyTo((Models.BioData)model);

            if (intent == MappingIntent.Query) bioModel.IsPersisted = true;
            bioModel.Owner = context.EntityMapper.ToModel<Models.User>(
                bioEntity.Owner,
                intent,
                context);

            bioModel.FirstName = bioEntity.FirstName;
            bioModel.MiddleName = bioEntity.MiddleName;
            bioModel.LastName = bioEntity.LastName;
            bioModel.DateOfBirth = bioEntity.DateOfBirth;
            bioModel.Sex = bioEntity.Sex;

            return bioModel;
        }

        public object ToEntity(
            object model,
            object entity,
            MappingIntent intent,
            MappingContext context)
        {
            var bioModel = (Models.BioData)model;
            var bioEntity = bioModel.CopyTo((Entities.BioData)entity);

            bioEntity.Owner = context.EntityMapper
                .ToEntity<Models.User>(
                    bioModel.Owner,
                    intent,
                    context)
                .As<Entities.User>();

            bioEntity.OwnerId = bioModel.Owner?.Id ?? Guid.Empty;
            bioEntity.FirstName = bioModel.FirstName;
            bioEntity.MiddleName = bioModel.MiddleName;
            bioEntity.LastName = bioModel.LastName;
            bioEntity.DateOfBirth = bioModel.DateOfBirth;
            bioEntity.Sex = bioModel.Sex;

            return bioEntity;
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
