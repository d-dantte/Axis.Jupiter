using Axis.Jupiter.Models;
using Axis.Jupiter.Services;
using System.Collections.Generic;
using System.Linq;

namespace Axis.Jupiter
{
    public static class Extensions
    {
        public static TModel[] TransformAll<TModel>(
            this IEnumerable<object> entities,
            TypeTransformer transformer)
        {
            var context = new TypeTransformContext {Transformer = transformer};

            return entities
                .Select(entity => transformer.ToModel<TModel>(entity, TransformCommand.Query, context))
                .ToArray();
        }
    }
}
