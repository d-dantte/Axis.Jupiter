using Axis.Jupiter.Helpers;
using Axis.Jupiter.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Axis.Jupiter
{
    public static class Extensions
    {
        public static TModel[] MapEntities<TModel>(
            this IEnumerable<object> entities,
            MappingIntent intent,
            EntityMapper mapper)
        => entities.MapEntities<TModel>(intent, new MappingContext(mapper));

        public static TModel[] MapEntities<TModel>(
            this IEnumerable<object> entities,
            MappingIntent intent,
            MappingContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            return entities
                .Select(entity => context.EntityMapper.ToModel<TModel>(entity, intent, context))
                .ToArray();
        }

        public static object[] MapModels<TModel>(
            this IEnumerable<TModel> models,
            MappingIntent intent,
            EntityMapper mapper)
        => models.MapModels(intent, new MappingContext(mapper));

        public static object[] MapModels<TModel>(
            this IEnumerable<TModel> models,
            MappingIntent intent,
            MappingContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            return models
                .Select(model => context.EntityMapper.ToEntity(model, intent, context))
                .ToArray();
        }

        public static async Task<IEnumerable<T>> Fold<T>(this IEnumerable<Task<T>> tasks)
        {
            await Task.WhenAll(tasks);
            return tasks.Select(t => t.Result);
        }

        public static Task Fold(this IEnumerable<Task> tasks) => Task.WhenAll(tasks);
    }
}
