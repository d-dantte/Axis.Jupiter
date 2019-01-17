using System.Collections.Generic;
using System.Linq;

namespace Axis.Jupiter
{
    public static class Extensions
    {
        public static TModel[] TransformQuery<TEntity, TModel>(
            this IEnumerable<TEntity> entities,
            ModelTransformer transformer,
            TransformCommand command)
        {
            var context = new ModelTransformationContext {Transformer = transformer};

            return entities
                .Select(entity => transformer.ToModel<TModel>(entity, command, context))
                .ToArray();
        }
    }
}
