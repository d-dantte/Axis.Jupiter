using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Axis.Jupiter.Helpers
{
    public interface IPropertyPath
    {
        IPropertyPath Parent { get; }
        Type OriginType { get; }
        Type FromType { get; }
        Type ToType { get; }
        string Property { get; }

        bool IsOrigin { get; }
    }

    public interface IPropertyPath<out TOrigin, out TTo> : IPropertyPath
    {
    }

    internal class PropertyPath<TOrigin, TTo> : IPropertyPath<TOrigin, TTo>
    {
        public  Type OriginType { get; }
        public Type FromType { get; }

        public Type ToType { get; }

        public string Property { get; }

        public IPropertyPath Parent { get; }

        public bool IsOrigin { get; internal set; }


        internal PropertyPath(IPropertyPath parent, string property, Type origin, Type from, Type to)
        {
            if (parent == null ^ string.IsNullOrWhiteSpace(property))
                throw new Exception("Invalid Parent/Property combination");

            Parent = parent;
            Property = property;

            OriginType = origin ?? throw new Exception("Invalid Origin-Type");
            FromType = from ?? throw new Exception("Invalid From-Type");
            ToType = to ?? throw new Exception("Invalid To-Type");
        }

        internal static PropertyPath<TOrigin, TOrigin> Origin()
        => new PropertyPath<TOrigin, TOrigin>(null, null, typeof(TOrigin), typeof(TOrigin), typeof(TOrigin))
        {
            IsOrigin = true
        };
    }

    public static class Paths
    {
        public static IPropertyPath<TOrigin, TNewTo> To<TOrigin, TTo, TNewTo>(this
            IPropertyPath<TOrigin, TTo> parent,
            Expression<Func<TTo, TNewTo>> newPath)
        {
            var propAccess = newPath?.Body as MemberExpression ?? throw new Exception("Invalid Path");

            var origin = typeof(TOrigin);
            var from = typeof(TTo);
            var to = typeof(TNewTo);
            var property = propAccess.Member.Name;

            return new PropertyPath<TOrigin, TNewTo>(parent, property, origin, from, to);
        }

        public static IPropertyPath<TOrigin, TNewTo> To<TOrigin, TTo, TNewTo>(this
            IPropertyPath<TOrigin, IEnumerable<TTo>> parent,
            Expression<Func<TTo, TNewTo>> newPath)
        {
            var propAccess = newPath?.Body as MemberExpression ?? throw new Exception("Invalid Path");

            var origin = typeof(TOrigin);
            var from = typeof(TTo);
            var to = typeof(TNewTo);
            var property = propAccess.Member.Name;

            return new PropertyPath<TOrigin, TNewTo>(parent, property, origin, from, to);
        }

        public static IPropertyPath<T, T> From<T>() => PropertyPath<T, T>.Origin();
    }
}
