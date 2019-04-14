using Axis.Jupiter.Contracts;
using Axis.Jupiter.Models;
using Axis.Luna.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Axis.Jupiter.Services
{
    public class TypeTransformer
    {
        private readonly TypeStoreProvider _storeProvider;

        public TypeTransformer(TypeStoreProvider storeProvider)
        {
            _storeProvider = storeProvider
                .ThrowIfNull(new ArgumentException("Invalid Store Provider"));
        }

        private static ITypeTransform ValidateTransform(ITypeTransform transform)
        {
            if (transform == null) throw new Exception("Invalid Transform: null");

            if (transform.EntityType == null) throw new Exception("Invalid Transform Entity Type: null");

            if (transform.ModelType == null) throw new Exception("Invalid Transform Model Type: null");

            if (transform.ToModel == null) throw new Exception("Invalid Entity-To-Model conversion: null");

            if (transform.ToEntity == null) throw new Exception("Invalid Model-To-Entity conversion: null");

            return transform;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="Model"></typeparam>
        /// <param name="model"></param>
        /// <param name="command"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public EntityGraph ToEntity<Model>(Model model, TransformCommand command, TypeTransformContext context = null)
        {
            if (model == null)
                return null;

            if (context?.Transformations.ContainsKey(model) == true)
                return (EntityGraph) context.Transformations[model];

            var transform = Transforms()
                .FirstOrDefault(t => t.ModelType == typeof(Model))
                .ThrowIfNull($"Transform not found for model: {typeof(Model).FullName}");

            var entity = new EntityGraph
            {
                Entity = transform.NewEntity != null
                    ? transform.NewEntity(model) ?? throw new Exception($"Entity creation failed for: {transform.EntityType.FullName}")
                    : Activator.CreateInstance(transform.EntityType)
            };

            if (context == null)
                context = new TypeTransformContext { Transformer = this };

            context.Transformations[model] = entity;

            return transform.ToEntity.Invoke(model, new EntityGraph { Entity = entity }, command, context);
        }

        public EntityGraph ToCollectionEntity<ParentModel, ChildModel>(
            ParentModel parent, 
            ChildModel child,
            string collectionProperty,
            TransformCommand command,
            TypeTransformContext context = null)
        {
            if (parent == null || child == null)
                return null;

            if (context?.Transformations.ContainsKey(child) == true)
                return (EntityGraph)context.Transformations[child];
            
            var transform = Transforms()
                .FirstOrDefault(t => t.ModelType == typeof(ChildModel))
                .ThrowIfNull($"Transform not found for model: {typeof(ChildModel).FullName}");

            var entityGraph = new EntityGraph
            {
                Entity = transform.NewEntity != null
                    ? transform.NewEntity(child) ?? throw new Exception($"Entity creation failed for: {transform.EntityType.FullName}")
                    : Activator.CreateInstance(transform.EntityType)
            };

            if (context == null)
                context = new TypeTransformContext { Transformer = this };

            context.Transformations[child] = entityGraph;

            return transform.ToCollectionEntity.Invoke(
                parent, 
                collectionProperty, 
                child, 
                entityGraph, 
                command, 
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
        public Model ToModel<Model>(object entity, TransformCommand command, TypeTransformContext context = null)
        {
            if (entity == null)
                return default(Model);

            if (context?.Transformations.ContainsKey(entity) == true)
                return (Model)context.Transformations[entity];

            var transform = Transforms()
                .FirstOrDefault(t => t.ModelType == typeof(Model))
                .ThrowIfNull($"Transform not found for model: {typeof(Model).FullName}");

            var model = transform.NewModel != null
                ? transform.NewModel(entity) ?? throw new Exception($"Model creation failed for: {transform.ModelType.FullName}")
                : Activator.CreateInstance(transform.ModelType);

            if (context == null)
                context = new TypeTransformContext { Transformer = this };

            context.Transformations[entity] = model;

            transform.ToModel.Invoke(entity, model, command, context);
            return (Model)model;
        }


        public ITypeTransform[] Transforms()
        => _storeProvider
            .Entries()
            .Select(_e => _e.TypeTransform)
            .ToArray();
    }

    public class TypeTransformContext
    {
        internal Dictionary<object, object> Transformations { get; } = new Dictionary<object, object>();
        public TypeTransformer Transformer { get; internal set; }

        internal TypeTransformContext()
        {
        }
    }
}
