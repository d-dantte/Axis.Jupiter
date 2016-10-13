using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using static Axis.Luna.Extensions.ExceptionExtensions;

namespace Axis.Jupiter.Europa.LinqProvider
{
    public class Provider : IQueryProvider
    {
        private EuropaContext _context = null;

        internal Provider(EuropaContext context)
        {
            ThrowNullArguments(() => context);

            this._context = context;
        }

        public Provider()
        { }


        public IQueryable CreateQuery(Expression expression)
        {
            var elementType = TypeSystem.GetElementType(expression.Type);
            try
            {
                return (IQueryable)Activator.CreateInstance(typeof(Queryable<>).MakeGenericType(elementType), new object[] { this, expression });
            }
            catch (TargetInvocationException tie)
            {
                throw tie.InnerException;
            }
        }

        public IQueryable<Data> CreateQuery<Data>(Expression expression) => new Queryable<Data>(this, expression);


        public object Execute(Expression expression)
        {
            throw new NotImplementedException();
        }

        public TResult Execute<TResult>(Expression expression)
        {
            throw new NotImplementedException();
        }

        //private Result Transform<Result>(Expression expression)
        //{

        //}
    }
}
