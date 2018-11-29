using System;
using System.Collections.Generic;
using System.Linq;
using Axis.Luna.Extensions;

namespace Axis.Jupiter
{
    public enum TransformCommand
    {
        Add,
        Delete,
        Update
    }

    public class ModelTransformer
    {
        private readonly IEnumerable<ModelTransform> _transforms;

        public ModelTransformer(IEnumerable<ModelTransform> transforms)
        {
            _transforms = transforms
                .ThrowIfNull(new ArgumentException("Invalid Transform list specified: null"))
                .Select(ValidateTransform)
                .ToArray();
        }

        private static ModelTransform ValidateTransform(ModelTransform transform)
        {
            if(transform == null) throw new Exception("Invalid Transform: null");

            if (transform.EntityType == null) throw new Exception("Invalid Transform Entity Type: null");

            if(transform.ModelType == null) throw new Exception("Invalid Transform Model Type: null");

            if(transform.EntityToModel == null) throw new Exception("Invalid Entity-To-Model conversion: null");

            if (transform.ModelToEntity == null) throw new Exception("Invalid Model-To-Entity conversion: null");

            return transform;
        }


        public object ToEntity<Model>(Model model, TransformCommand command, ModelTransformationContext context = null)
        {
            if (model == null)
                return null;

            if (context?.Transformations.ContainsKey(model) == true)
                return context.Transformations[model];

            var transform = _transforms
                .FirstOrDefault(t => t.ModelType == typeof(Model))
                .ThrowIfNull($"Transform not found for model: {typeof(Model).FullName}");

            var entity = transform.CreateEntity != null
                ? transform.CreateEntity(model) ?? throw new Exception($"Entity creation failed for: {transform.EntityType.FullName}")
                : Activator.CreateInstance(transform.EntityType);

            if (context == null)
                context = new ModelTransformationContext {Transformer = this};
            context.Transformations[model] = entity;

            transform.ModelToEntity.Invoke(model, entity, command, context);
            return entity;
        }

        public Model ToModel<Model>(object entity, TransformCommand command, ModelTransformationContext context = null)
        {
            if (entity == null)
                return default(Model);

            if (context?.Transformations.ContainsKey(entity) == true)
                return (Model) context.Transformations[entity];

            var transform = _transforms
                .FirstOrDefault(t => t.ModelType == typeof(Model))
                .ThrowIfNull($"Transform not found for model: {typeof(Model).FullName}");

            var model = transform.CreateModel != null
                ? transform.CreateModel(entity) ?? throw new Exception($"Model creation failed for: {transform.ModelType.FullName}")
                : Activator.CreateInstance(transform.ModelType);

            if (context == null)
                context = new ModelTransformationContext { Transformer = this };
            context.Transformations[entity] = model;

            transform.EntityToModel.Invoke(entity, model, command, context);
            return (Model) model;
        }

        public Entity ToEntity<Model, Entity>(Model model, TransformCommand command, ModelTransformationContext context = null)
        {
            if (model == null)
                return default(Entity);

            if (context?.Transformations.ContainsKey(model) == true)
                return (Entity) context.Transformations[model];

            var transform = _transforms
                .FirstOrDefault(t => t.EntityType == typeof(Entity))
                .ThrowIfNull($"Transform not found for entity: {typeof(Entity).FullName}");

            var entity = transform.CreateEntity != null
                ? transform.CreateEntity(model) ?? throw new Exception($"Entity creation failed for: {transform.EntityType.FullName}")
                : Activator.CreateInstance(transform.EntityType);

            if (context == null)
                context = new ModelTransformationContext { Transformer = this };
            context.Transformations[model] = entity;

            transform.ModelToEntity.Invoke(model, entity, command, context);
            return (Entity) entity;
        }

        public Model ToModel<Entity, Model>(Entity entity, TransformCommand command, ModelTransformationContext context = null)
        {
            if (entity == null)
                return default(Model);

            if (context?.Transformations.ContainsKey(entity) == true)
                return (Model) context.Transformations[entity];

            var transform = _transforms
                .FirstOrDefault(t => t.EntityType == typeof(Entity))
                .ThrowIfNull($"Transform not found for entity: {typeof(Entity).FullName}");

            var model = transform.CreateModel != null
                ? transform.CreateModel(entity) ?? throw new Exception($"Model creation failed for: {transform.ModelType.FullName}")
                : Activator.CreateInstance(transform.ModelType);

            if (context == null)
                context = new ModelTransformationContext { Transformer = this };
            context.Transformations[entity] = model;

            transform.EntityToModel.Invoke(entity, model, command, context);
            return (Model) model;
        }

        public ModelTransform[] Transforms() => _transforms.ToArray();
    }

    public class ModelTransformationContext
    {
        internal Dictionary<object, object> Transformations { get; } = new Dictionary<object, object>();
        public  ModelTransformer Transformer { get; internal set; }

        internal ModelTransformationContext()
        {
        }
    }

    public class ModelTransform
    {
        public Type ModelType { get; set; }
        public Type EntityType { get; set; }

        public Action<object, object, TransformCommand, ModelTransformationContext> ModelToEntity { get; set; }
        public Action<object, object, TransformCommand, ModelTransformationContext> EntityToModel { get; set; }
        public Func<object, object> CreateEntity { get; set; }
        public Func<object, object> CreateModel { get; set; }

        public static ModelTransform IdempotentTransform<T>()
        {
            return new ModelTransform
            {
                ModelType = typeof(T),
                EntityType = typeof(T),
                CreateModel = (t) => t,
                CreateEntity = (t) => t,
                ModelToEntity = (model, entity, command, context) => { },
                EntityToModel = (entity, model, command, context) => { }
            };
        }
    }
}
