using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using SimpleLINQ.Async.Internal;
using System.Linq;

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
