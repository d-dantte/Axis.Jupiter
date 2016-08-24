using Axis.Jupiter.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Axis.Jupiter
{
    public static class Extensions
    {
        public static SequencePage<Data> Paginate<Data>(this IEnumerable<Data> sequence, int pageIndex, int pageSize)
            => new SequencePage<Data>(sequence.Skip(pageSize * pageIndex).Take(pageSize).ToArray(),
                                      pageIndex,
                                      sequence.Count());

        public static SequencePage<Data> Paginate<Data, OrderKey>(this IQueryable<Data> sequence,
                                                                       int pageIndex,
                                                                       int pageSize,
                                                                       Expression<Func<Data, OrderKey>> orderExpression)
            => new SequencePage<Data>(sequence.OrderBy(orderExpression).Skip(pageSize * pageIndex).Take(pageSize).ToArray(),
                                      pageIndex,
                                      sequence.Count());

        public static SequencePage<Data> PaginateDescending<Data, OrderKey>(this IQueryable<Data> sequence,
                                                                                 int pageIndex,
                                                                                 int pageSize,
                                                                                 Expression<Func<Data, object>> orderExpression)
            => new SequencePage<Data>(sequence.OrderByDescending(orderExpression).Skip(pageSize * pageIndex).Take(pageSize).ToArray(),
                                      pageIndex,
                                      sequence.Count());
    }
}
