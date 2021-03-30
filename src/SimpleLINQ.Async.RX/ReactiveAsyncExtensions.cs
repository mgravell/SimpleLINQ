using SimpleLINQ.Async.Internal;
using System;
using System.Linq;

namespace SimpleLINQ.Async
{
    /// <summary>
    /// Utility methods for working with SimpleLINQ queries via Reactive async APIs
    /// </summary>
    public static class ReactiveAsyncExtensions
    {
        /// <summary>
        /// Wraps a query with an <see cref="IAsyncQueryable{T}"/> wrapper, allowing <see cref="AsyncEnumerable"/> methods to work.
        /// </summary>
        public static IAsyncQueryable<T> AsAsyncQueryable<T>(this IQueryable<T> source) => source switch
        {
            IAsyncQueryable<T> already => already,
            Query query => new AsyncQuery<T>(query),
            _ => throw new ArgumentException("The source is not a SimpleLINQ query", nameof(source)),
        };
    }
}