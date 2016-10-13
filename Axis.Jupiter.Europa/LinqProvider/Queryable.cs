using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using static Axis.Luna.Extensions.ExceptionExtensions;

namespace Axis.Jupiter.Europa.LinqProvider
{
    public class Queryable<Data> : IOrderedQueryable<Data>
    {
        #region Constructors
        /// <summary> 
        /// This constructor is called by the client to create the data source. 
        /// </summary> 
        public Queryable()
        : this(new Provider())
        { }

        /// <summary> 
        /// This constructor is called by the client to create the data source. 
        /// </summary> 
        public Queryable(Provider provider)
        {
            ThrowNullArguments(() => provider);

            Provider = provider ?? new Provider();
            Expression = Expression.Constant(this);
        }

        /// <summary> 
        /// This constructor is called by Provider.CreateQuery(). 
        /// </summary> 
        /// <param name="expression"></param>
        public Queryable(Provider provider, Expression expression)
        {
            ThrowNullArguments(() => provider, () => expression);

            if (!typeof(IQueryable<Data>).IsAssignableFrom(expression.Type))
                throw new ArgumentOutOfRangeException("expression");

            Provider = provider;
            Expression = expression;
        }
        #endregion

        #region Properties

        public IQueryProvider Provider { get; private set; }
        public Expression Expression { get; private set; }

        public Type ElementType => typeof(Data);

        #endregion

        #region Enumerators
        public IEnumerator<Data> GetEnumerator() => (Provider.Execute<IEnumerable<Data>>(Expression)).GetEnumerator();

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => this.GetEnumerator();
        
        #endregion
    }
}
