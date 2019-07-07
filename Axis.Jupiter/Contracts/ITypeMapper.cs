using Axis.Jupiter.Helpers;
using Axis.Jupiter.Models;
using System;
using System.Collections.Generic;

namespace Axis.Jupiter.Contracts
{
    public interface ITypeMapper
    {
        Type ModelType { get; }
        Type EntityType { get; }

        /// <summary>
        /// A function that, given a Model instance, creates an Entity Instance. This property is optional.
        /// </summary>
        object NewEntity(object model);

        /// <summary>
        /// A function that, given an Entity instance, creates a Model Instance. This property is optional.
        /// </summary>
        object NewModel(object entity);

        /// <summary>
        /// A function that, given:
        /// 1. Entity Instance (not an EntityGraph),
        /// 2. Model Instance,
        /// 3. TransformCommand,
        /// 4. TransformContext,
        /// Returns the result of copying to the given Model (and it's possible related entities) the appropriate properties/values from the Entity
        /// with as much as possible of the Entity's relationships translated as well.
        /// </summary>
        object ToModel(
            object entity,
            object model,
            MappingIntent intent,
            MappingContext context);

        /// <summary>
        /// A function that, given:
        /// 1. Model Instance,
        /// 2. EntityGraph Instance,
        /// 3. TransformCommand,
        /// 4. TransformContext,
        /// Returns the result of copying to the given entity (and it's possible related entities) the appropriate properties/values from the Model
        /// with as much as possible of the Model's relationships translated as well.
        /// </summary>
        object ToEntity(
            object model,
            object entity,
            MappingIntent intent,
            MappingContext context);


        /// <summary>
        /// A function that, given:
        /// 1. A Parent Model instance (the object who 'owns' the list into which the object is being added),
        /// 2. Mappingintent (only Add and Remove values are accepted),
        /// 3. The name of the property housing the list on the parent object,
        /// 4. The models to be mapped
        /// 5. TransformContext,
        /// Returns a list of CollectionRefInfo objects that wrap around an entity, Each of which represents a multipliciy relationship
        /// to another entity. These entities have to either be added/removed - in cases of join-lists, or update - in cases of 
        /// foreign-relationships.
        /// </summary>
        IEnumerable<CollectionRefInfo> ToCollectionRefInfos<TModel>(
            object parentModel,
            MappingIntent intent,
            string propertyName,
            TModel[] children,
            MappingContext context)
            where TModel: class;
    }
}
