using System;
using System.Linq;
using System.Linq.Expressions;

namespace Auth.Common.Extensions
{
    public static class IQueryableExtensions
    {
        public static IQueryable<TSource> WhereIf<TSource>(
        this IQueryable<TSource> source,
        bool condition,
        Func<IQueryable<TSource>, IQueryable<TSource>> branch)
        {
            return condition ? branch(source) : source;
        }

        public static IQueryable<TSource> IncludeIf<TSource>(
            this IQueryable<TSource> source,
            bool condition,
            Func<IQueryable<TSource>, IQueryable<TSource>> branch)
        {
            return condition ? branch(source) : source;
        }


        public static IOrderedQueryable<T> OrderBy<T>(this
            IQueryable<T> source, string propertyName, bool ascending = true)
        {
            if (ascending) return source.OrderBy(ToLambda<T>(propertyName));

            return source.OrderByDescending(ToLambda<T>(propertyName));
        }


        public static IOrderedQueryable<T>
            OrderByDescending<T>(this IQueryable<T> source, string
            propertyName, bool ascending = true)
        {
            if (ascending) return source.OrderBy(ToLambda<T>(propertyName));

            return source.OrderByDescending(ToLambda<T>(propertyName));
        }


        public static IOrderedQueryable<T> ThenBy<T>(this
            IOrderedQueryable<T> source, string propertyName, bool ascending =
            true)
        {
            if (ascending) return source.ThenBy(ToLambda<T>(propertyName));

            return source.ThenByDescending(ToLambda<T>(propertyName));
        }


        public static IOrderedQueryable<T> ThenByDescending<T>(this
            IOrderedQueryable<T> source, string propertyName, bool ascending =
            true)
        {
            if (ascending) return source.ThenBy(ToLambda<T>(propertyName));

            return source.ThenByDescending(ToLambda<T>(propertyName));
        }

        private static Expression<Func<T, object>> ToLambda<T>(string propertyName)
        {
            var parameter = Expression.Parameter(typeof(T));
            var property = Expression.Property(parameter, propertyName);
            var propAsObject = Expression.Convert(property, typeof(object));

            return Expression.Lambda<Func<T, object>>(propAsObject, parameter);
        }
    }
}
