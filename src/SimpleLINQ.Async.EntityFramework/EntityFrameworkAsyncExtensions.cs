using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using SimpleLINQ.Async.Internal;
using System.Linq;

// note on namespace choice: Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions and SimpleLINQ.Async.QueryableAsyncExtensions both
// have .CountAsync(this IQueryable<T>) etc; if we just used SimpleLINQ.Async, then this could cause problems; the idea here is that someone
// using Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions can use SimpleLINQ.Async.EntityFrameworkCore but not SimpleLINQ.Async,
// meaning that they *avoid* any conflicts
namespace SimpleLINQ.Async.EntityFrameworkCore
{
    /// <summary>
    /// Utility methods for working with SimpleLINQ queries via EF-Core async APIs
    /// </summary>
    public static class EntityFrameworkAsyncExtensions
    {
        /// <summary>
        /// Wraps a query with an <see cref="IAsyncQueryProvider"/> async query provider, allowing <see cref="EntityFrameworkQueryableExtensions"/> methods to work.
        /// </summary>
        public static IQueryable<T> PrepareForAsync<T>(this IQueryable<T> source)
        {
            if (source is Query query && source.Provider is not IAsyncQueryProvider)
            {   // wrap it
                return new AsyncQuery<T>(query);
            }

            return source;
        }
    }
}
