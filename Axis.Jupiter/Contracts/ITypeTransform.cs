using Axis.Jupiter.Models;
using Axis.Jupiter.Services;
using System;

namespace Axis.Jupiter.Contracts
{
    public interface ITypeTransform
    {
        Type ModelType { get; }
        Type EntityType { get; }

        /// <summary>
        /// A function that, given a Model instance, creates an Entity Instance. This property is optional.
        /// </summary>
        Func<object, object> NewEntity { get; }

        /// <summary>
        /// A function that, given an Entity instance, creates a Model Instance. This property is optional.
        /// </summary>
        Func<object, object> NewModel { get; }

        /// <summary>
        /// A function that, given:
        /// 1. Entity Instance (not an EntityGraph),
        /// 2. Model Instance,
        /// 3. TransformCommand,
        /// 4. TransformContext,
        /// Returns the result of copying to the given Model (and it's possible related entities) the appropriate properties/values from the Entity
        /// with as much as possible of the Entity's relationships translated as well.
        /// </summary>
        Func<object, object, TransformCommand, TypeTransformContext, object> ToModel { get; }

        /// <summary>
        /// A function that, given:
        /// 1. Model Instance,
        /// 2. EntityGraph Instance,
        /// 3. TransformCommand,
        /// 4. TransformContext,
        /// Returns the result of copying to the given entity (and it's possible related entities) the appropriate properties/values from the Model
        /// with as much as possible of the Model's relationships translated as well.
        /// </summary>
        Func<object, EntityGraph, TransformCommand, TypeTransformContext, EntityGraph> ToEntity { get; }

        /// <summary>
        /// A function that, given:
        /// 1. A parent Model instance (the object who 'owns' the list into which the object is being added),
        /// 2. The name of the property housing the list on the parent object,
        /// 3. Model Instance,
        /// 4. EntityGraph Instance,
        /// 5. TransformCommand (only Add and Remove values are accepted),
        /// 6. TransformContext,
        /// Returns the result of copying to the given entity (and it's possible related entities) the appropriate properties/values from the Model
        /// with as much as possible of the Model's relationships translated as well.
        /// </summary>
        Func<object, string, object, EntityGraph, TransformCommand, TypeTransformContext, EntityGraph> ToCollectionEntity { get; }
    }
}
