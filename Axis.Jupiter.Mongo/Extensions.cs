using Axis.Jupiter.Helpers;
using Axis.Jupiter.MongoDb.Providers;
using Axis.Jupiter.MongoDb.XModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Axis.Jupiter.MongoDb
{
    public static class Extensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="info"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static EntityRef<TEntity, TKey> CreateRef<TEntity, TKey>(
            this EntityInfoProvider provider,
            TKey key)
        where TEntity : IMongoEntity<TKey>, new()
        {
            var info = provider.InfoForEntity<TEntity>();

            return new EntityRef<TEntity, TKey>(
                key,
                info.CollectionName,
                info.Database);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="info"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static EntityRef<TEntity, TKey> CreateRef<TEntity, TKey>(
            this EntityInfoProvider provider,
            TEntity entity)
        where TEntity : IMongoEntity<TKey>, new()
        {
            var info = provider.InfoForEntity<TEntity>();

            return new EntityRef<TEntity, TKey>(
                entity,
                info.CollectionName,
                info.Database);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="info"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static EntitySetRef<TRefInstance, TRefKey, TSourceKey> CreateSetRef<TRefInstance, TRefKey, TSourceKey>(
            this EntityInfoProvider provider,
            TSourceKey sourceKey,
            string refLabel,
            params TRefKey[] keys)
        where TRefInstance : IMongoEntity<TRefKey>, new()
        {
            var info = provider.InfoForSetEntity<TRefInstance, TRefKey, TSourceKey>();

            return new EntitySetRef<TRefInstance, TRefKey, TSourceKey>(
                info.CollectionName,
                info.Database,
                keys.Select(refKey => 
                    new SetRefEntity<TRefInstance, TRefKey, TSourceKey>(
                        sourceKey,
                        refKey,
                        refLabel))
                    .ToArray());
        }


        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="info"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static EntitySetRef<TRefInstance, TRefKey, TSourceKey> CreateSetRef<TRefInstance, TRefKey, TSourceKey>(
            this EntityInfoProvider provider,
            TSourceKey sourceKey,
            string refLabel,
            params TRefInstance[] entities)
        where TRefInstance : IMongoEntity<TRefKey>, new()
        {
            var info = provider.InfoForSetEntity<TRefInstance, TRefKey, TSourceKey>();

            return new EntitySetRef<TRefInstance, TRefKey, TSourceKey>(
                info.CollectionName,
                info.Database,
                entities.Select(refInstance =>
                    new SetRefEntity<TRefInstance, TRefKey, TSourceKey>(
                        sourceKey,
                        provider.CreateRef<TRefInstance, TRefKey>(refInstance),
                        refLabel))
                    .ToArray());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TRefInstance"></typeparam>
        /// <typeparam name="TRefKey"></typeparam>
        /// <typeparam name="TSourceKey"></typeparam>
        /// <param name="provider"></param>
        /// <param name="sourceKey"></param>
        /// <param name="refKey"></param>
        /// <param name="refLabel"></param>
        /// <returns></returns>
        public static SetRefEntity<TRefInstance, TRefKey, TSourceKey> CreateSetRefEntity<TRefInstance, TRefKey, TSourceKey>(
            this EntityInfoProvider provider,
            TSourceKey sourceKey,
            TRefKey refKey,
            string refLabel)
            where TRefInstance : IMongoEntity<TRefKey>
        {
            var info = provider.InfoForSetEntity<TRefInstance, TRefKey, TSourceKey>();

            return new SetRefEntity<TRefInstance, TRefKey, TSourceKey>(
                sourceKey,
                refKey,
                refLabel);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="entities"></param>
        /// <param name="intent"></param>
        /// <returns></returns>
        public static IEnumerable<TEntity> FilterForIntent<TEntity>(
            this IEnumerable<TEntity> entities, 
            MappingIntent intent)
            where TEntity : IMongoEntity
        {
            return entities.Where(entity =>
            {
                switch(intent)
                {
                    case MappingIntent.Add:
                        return !entity.IsPersisted;

                    case MappingIntent.Update:
                    case MappingIntent.Remove:
                        return entity.IsPersisted;

                    default:
                        return true;
                }
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enm"></param>
        /// <param name="addendum"></param>
        /// <returns></returns>
        public static IEnumerable<T> Concatenate<T>(this IEnumerable<T> enm, params T[] addendum)
        {
            foreach (var t in enm)
                yield return t;

            foreach (var t in addendum)
                yield return t;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="genericTypeDefinition"></param>
        /// <returns></returns>
        public static bool TryGetGenericTypeDefinition(this Type type, out Type genericTypeDefinition)
        {
            if(type.IsGenericType)
            {
                genericTypeDefinition = type.GetGenericTypeDefinition();
                return true;
            }
            else
            {
                genericTypeDefinition = null;
                return false;
            }
        }
    }
}
