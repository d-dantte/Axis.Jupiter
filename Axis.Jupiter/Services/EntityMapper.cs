using Axis.Jupiter.Configuration;
using Axis.Jupiter.Contracts;
using Axis.Jupiter.Helpers;
using Axis.Jupiter.Models;
using Axis.Jupiter.Providers;
using Axis.Luna.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Axis.Jupiter.Services
{
    public class EntityMapper
    {
        private readonly TypeStoreProvider _storeProvider;

        public EntityMapper(TypeStoreProvider storeProvider)
        {
            _storeProvider = storeProvider
                .ThrowIfNull(new ArgumentException("Invalid Store Provider"));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="Model"></typeparam>
        /// <param name="model"></param>
        /// <param name="command"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public object ToEntity<Model>(Model model, MappingIntent command, MappingContext context = null)
        {
            if (model == null)
                return null;

            if (context?.Mappers.ContainsKey(model) == true)
                return context.Mappers[model];

            var transform = Mappers()
                .FirstOrDefault(t => t.ModelType == typeof(Model))
                .ThrowIfNull($"Transform not found for model: {typeof(Model).FullName}");

            var entity = transform.NewEntity(model) ?? throw new Exception($"Entity creation failed for: {transform.EntityType.FullName}");

            if (context == null)
                context = new MappingContext(this);

            context.Mappers[model] = entity;

            return transform.ToEntity(model, entity, command, context);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="ParentModel"></typeparam>
        /// <param name="parentModel"></param>
        /// <param name="collectionProperty"></param>
        /// <param name="command"></param>
        /// <returns></returns>
        public IEnumerable<CollectionRefInfo> ToCollectionRefInfo<ParentModel, ChildModel>(
            ParentModel parentModel,
            MappingIntent intent,
            Expression<Func<ParentModel, ICollection<ChildModel>>> collectionProperty,
            ChildModel[] children)
            where ChildModel : class
        {
            if (parentModel == null)
                throw new Exception("Invalid parent model");

            var property = collectionProperty.Body
                .As<MemberExpression>().Member
                .As<PropertyInfo>();

            if (intent != MappingIntent.Add
                && intent != MappingIntent.Remove)
                throw new Exception("Invalid Intent: " + intent);
            
            var parentMapper = Mappers()
                .FirstOrDefault(t => t.ModelType == typeof(ParentModel))
                .ThrowIfNull($"Mapper not found for model: {typeof(ParentModel).FullName}");

            var context = new MappingContext(this);

            return parentMapper.ToCollectionRefInfos(
                parentModel,
                intent,
                property.Name,
                children,
                context);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="Model"></typeparam>
        /// <param name="entity"></param>
        /// <param name="command"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public Model ToModel<Model>(
            object entity, 
            MappingIntent command, 
            MappingContext context = null)
        {
            if (entity == null)
                return default(Model);

            if (context?.Mappers.ContainsKey(entity) == true)

                return (Model)context.Mappers[entity];

            var mapper = Mappers()
                .FirstOrDefault(t => t.ModelType == typeof(Model))
                .ThrowIfNull($"Mapper not found for model: {typeof(Model).FullName}");

            var model = mapper.NewModel(entity) ?? throw new Exception($"Model creation failed for: {mapper.ModelType.FullName}");

            if (context == null)
                context = new MappingContext(this);

            context.Mappers[entity] = model;

            mapper.ToModel(entity, model, command, context);
            return (Model)model;
        }

        public ITypeMapper[] Mappers()
        => _storeProvider
            .Entries()
            .Select(_e => _e.TypeMapper)
            .ToArray();

        public TypeStoreEntry[] Entries()
        => _storeProvider
            .Entries()
            .ToArray();

        public TypeStoreEntry EntryForEntity<EntityType>()
        => _storeProvider
            .Entries()
            .FirstOrDefault(e => e.TypeMapper.EntityType == typeof(EntityType));

        public TypeStoreEntry EntryForModel<ModelType>()
        => _storeProvider
            .Entries()
            .FirstOrDefault(e => e.TypeMapper.ModelType == typeof(ModelType));

        public TypeStoreEntry EntryForEntity(Type entityType)
        => _storeProvider
            .Entries()
            .FirstOrDefault(e => e.TypeMapper.EntityType == entityType);

        public TypeStoreEntry EntryForModel(Type modelType)
        => _storeProvider
            .Entries()
            .FirstOrDefault(e => e.TypeMapper.ModelType == modelType);

    }
}
