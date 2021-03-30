using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleLINQ.Async
{
    /// <summary>
    /// Utility methods for working with queries in an asynchronous way
    /// </summary>
    public static class QueryableAsyncExtensions
    {
        /// <summary>
        /// Converts a synchronous query to an asynchronous sequence
        /// </summary>
        public static IAsyncEnumerable<TSource> AsAsyncEnumerable<TSource>(this IQueryable<TSource> source)
        {
            return source as IAsyncEnumerable<TSource> ?? Wrap(source, default);

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously - interntional
            async static IAsyncEnumerable<TSource> Wrap(IEnumerable<TSource> source, [EnumeratorCancellation] CancellationToken cancellationToken)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
            {
                foreach (var item in source)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    yield return item;
                }
            }
        }

        private static Query GetQuery(IQueryable source)
            => source as Query ?? throw new ArgumentException($"The source provided is not a SimpleLINQ query", nameof(source));

        /// <summary>
        /// Asynchronous version of <see cref="Queryable.Any{TSource}(IQueryable{TSource})"/>.
        /// </summary>
        public static ValueTask<bool> AnyAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default)
        {
            var query = GetQuery(source);
            return query.Provider.ExecuteAggregateAsync<bool>(query, Aggregate.Any, cancellationToken);
        }

        /// <summary>
        /// Asynchronous version of <see cref="Queryable.Any{TSource}(IQueryable{TSource}, Expression{Func{TSource, bool}})"/>.
        /// </summary>
        public static ValueTask<bool> AnyAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken cancellationToken = default)
        {
            var query = GetQuery(source).ApplyWhere(predicate);
            return query.Provider.ExecuteAggregateAsync<bool>(query, Aggregate.Any, cancellationToken);
        }

        /// <summary>
        /// Asynchronous version of <see cref="Queryable.All{TSource}(IQueryable{TSource}, Expression{Func{TSource, bool}})"/>.
        /// </summary>
        public static ValueTask<bool> AllAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken cancellationToken = default)
        {
            var query = GetQuery(source).ApplyWhere(predicate.Negate());
            return query.Provider.ExecuteAggregateAsync<bool>(query, Aggregate.NotAny, cancellationToken);
        }

        /// <summary>
        /// Asynchronous version of <see cref="Queryable.Count{TSource}(IQueryable{TSource})"/>.
        /// </summary>
        public static ValueTask<int> CountAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default)
        {
            var query = GetQuery(source);
            return query.Provider.ExecuteAggregateAsync<int>(query, Aggregate.Count, cancellationToken);
        }

        /// <summary>
        /// Asynchronous version of <see cref="Queryable.LongCount{TSource}(IQueryable{TSource})"/>.
        /// </summary>
        public static ValueTask<long> LongCountAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default)
        {
            var query = GetQuery(source);
            return query.Provider.ExecuteAggregateAsync<long>(query, Aggregate.Count, cancellationToken);
        }

        /// <summary>
        /// Asynchronous version of <see cref="Queryable.Count{TSource}(IQueryable{TSource}, Expression{Func{TSource, bool}})"/>.
        /// </summary>
        public static ValueTask<int> CountAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken cancellationToken = default)
        {
            var query = GetQuery(source).ApplyWhere(predicate);
            return query.Provider.ExecuteAggregateAsync<int>(query, Aggregate.Count, cancellationToken);
        }

        /// <summary>
        /// Asynchronous version of <see cref="Queryable.LongCount{TSource}(IQueryable{TSource}, Expression{Func{TSource, bool}})"/>.
        /// </summary>
        public static ValueTask<long> LongCountAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken cancellationToken = default)
        {
            var query = GetQuery(source).ApplyWhere(predicate);
            return query.Provider.ExecuteAggregateAsync<long>(query, Aggregate.Count, cancellationToken);
        }

        /// <summary>
        /// Asynchronous version of <see cref="Queryable.First{TSource}(IQueryable{TSource})"/>.
        /// </summary>
        public static ValueTask<TSource> FirstAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default)
        {
            var query = GetQuery(source);
            return query.Provider.ExecuteAggregateAsync<TSource>(query, Aggregate.First, cancellationToken);
        }

        /// <summary>
        /// Asynchronous version of <see cref="Queryable.First{TSource}(IQueryable{TSource}, Expression{Func{TSource, bool}})"/>.
        /// </summary>
        public static ValueTask<TSource> FirstAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken cancellationToken = default)
        {
            var query = GetQuery(source).ApplyWhere(predicate);
            return query.Provider.ExecuteAggregateAsync<TSource>(query, Aggregate.First, cancellationToken);
        }

        /// <summary>
        /// Asynchronous version of <see cref="Queryable.FirstOrDefault{TSource}(IQueryable{TSource})"/>.
        /// </summary>
        public static ValueTask<TSource> FirstOrDefaultAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default)
        {
            var query = GetQuery(source);
            return query.Provider.ExecuteAggregateAsync<TSource>(query, Aggregate.FirstOrDefault, cancellationToken);
        }

        /// <summary>
        /// Asynchronous version of <see cref="Queryable.FirstOrDefault{TSource}(IQueryable{TSource}, Expression{Func{TSource, bool}})"/>.
        /// </summary>
        public static ValueTask<TSource> FirstOrDefaultAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken cancellationToken = default)
        {
            var query = GetQuery(source).ApplyWhere(predicate);
            return query.Provider.ExecuteAggregateAsync<TSource>(query, Aggregate.FirstOrDefault, cancellationToken);
        }

        /// <summary>
        /// Asynchronous version of <see cref="Queryable.Single{TSource}(IQueryable{TSource})"/>.
        /// </summary>
        public static ValueTask<TSource> SingleAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default)
        {
            var query = GetQuery(source);
            return query.Provider.ExecuteAggregateAsync<TSource>(query, Aggregate.Single, cancellationToken);
        }

        /// <summary>
        /// Asynchronous version of <see cref="Queryable.Single{TSource}(IQueryable{TSource}, Expression{Func{TSource, bool}})"/>.
        /// </summary>
        public static ValueTask<TSource> SingleAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken cancellationToken = default)
        {
            var query = GetQuery(source).ApplyWhere(predicate);
            return query.Provider.ExecuteAggregateAsync<TSource>(query, Aggregate.Single, cancellationToken);
        }

        /// <summary>
        /// Asynchronous version of <see cref="Queryable.SingleOrDefault{TSource}(IQueryable{TSource})"/>.
        /// </summary>
        public static ValueTask<TSource> SingleOrDefaultAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default)
        {
            var query = GetQuery(source);
            return query.Provider.ExecuteAggregateAsync<TSource>(query, Aggregate.SingleOrDefault, cancellationToken);
        }

        /// <summary>
        /// Asynchronous version of <see cref="Queryable.SingleOrDefault{TSource}(IQueryable{TSource}, Expression{Func{TSource, bool}})"/>.
        /// </summary>
        public static ValueTask<TSource> SingleOrDefaultAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken cancellationToken = default)
        {
            var query = GetQuery(source).ApplyWhere(predicate);
            return query.Provider.ExecuteAggregateAsync<TSource>(query, Aggregate.SingleOrDefault, cancellationToken);
        }

        /// <summary>
        /// Asynchronous version of <see cref="Queryable.Last{TSource}(IQueryable{TSource})"/>.
        /// </summary>
        public static ValueTask<TSource> LastAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default)
        {
            var query = GetQuery(source).ApplyReverse();
            return query.Provider.ExecuteAggregateAsync<TSource>(query, Aggregate.First, cancellationToken);
        }

        /// <summary>
        /// Asynchronous version of <see cref="Queryable.Last{TSource}(IQueryable{TSource}, Expression{Func{TSource, bool}})"/>.
        /// </summary>
        public static ValueTask<TSource> LastAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken cancellationToken = default)
        {
            var query = GetQuery(source).ApplyWhere(predicate).ApplyReverse();
            return query.Provider.ExecuteAggregateAsync<TSource>(query, Aggregate.First, cancellationToken);
        }

        /// <summary>
        /// Asynchronous version of <see cref="Queryable.LastOrDefault{TSource}(IQueryable{TSource})"/>.
        /// </summary>
        public static ValueTask<TSource> LastOrDefaultAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default)
        {
            var query = GetQuery(source).ApplyReverse();
            return query.Provider.ExecuteAggregateAsync<TSource>(query, Aggregate.FirstOrDefault, cancellationToken);
        }

        /// <summary>
        /// Asynchronous version of <see cref="Queryable.LastOrDefault{TSource}(IQueryable{TSource}, Expression{Func{TSource, bool}})"/>.
        /// </summary>
        public static ValueTask<TSource> LastOrDefaultAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken cancellationToken = default)
        {
            var query = GetQuery(source).ApplyWhere(predicate).ApplyReverse();
            return query.Provider.ExecuteAggregateAsync<TSource>(query, Aggregate.FirstOrDefault, cancellationToken);
        }

        /// <summary>
        /// Asynchronous version of <see cref="Enumerable.ToList{TSource}(IEnumerable{TSource})"/>.
        /// </summary>
        public static ValueTask<List<TSource>> ToListAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default)
        {
            var query = GetQuery(source);
            if (query.Take == 0)
                return new(new List<TSource>());
            return ToListCoreAsync<TSource>(query, cancellationToken);
        }

        /// <summary>
        /// Asynchronous version of <see cref="Enumerable.ToArray{TSource}(IEnumerable{TSource})"/>.
        /// </summary>
        public static ValueTask<TSource[]> ToArrayAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default)
        {
            var query = GetQuery(source);
            if (query.Take == 0)
                return new(Array.Empty<TSource>());
            return ToArrayCoreAsync<TSource>(query, cancellationToken);
        }

        /// <summary>
        /// Asynchronous version of <see cref="Queryable.Min{TSource}(IQueryable{TSource})"/>.
        /// </summary>
        public static ValueTask<TSource> MinAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default)
        {
            var query = GetQuery(source);
            return query.Provider.ExecuteAggregateAsync<TSource>(query, Aggregate.Minimum, cancellationToken);
        }

        /// <summary>
        /// Asynchronous version of <see cref="Queryable.Min{TSource, TResult}(IQueryable{TSource}, Expression{Func{TSource, TResult}})"/>.
        /// </summary>
        public static ValueTask<TValue> MinAsync<TSource, TValue>(this IQueryable<TSource> source, Expression<Func<TSource, TValue>> selector, CancellationToken cancellationToken = default)
        {
            var query = GetQuery(source).ApplySelect(selector);
            return query.Provider.ExecuteAggregateAsync<TValue>(query, Aggregate.Minimum, cancellationToken);
        }

        /// <summary>
        /// Asynchronous version of <see cref="Queryable.Max{TSource}(IQueryable{TSource})"/>.
        /// </summary>
        public static ValueTask<TSource> MaxAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default)
        {
            var query = GetQuery(source);
            return query.Provider.ExecuteAggregateAsync<TSource>(query, Aggregate.Minimum, cancellationToken);
        }

        /// <summary>
        /// Asynchronous version of <see cref="Queryable.Max{TSource, TResult}(IQueryable{TSource}, Expression{Func{TSource, TResult}})"/>.
        /// </summary>
        public static ValueTask<TValue> MaxAsync<TSource, TValue>(this IQueryable<TSource> source, Expression<Func<TSource, TValue>> selector, CancellationToken cancellationToken = default)
        {
            var query = GetQuery(source).ApplySelect(selector);
            return query.Provider.ExecuteAggregateAsync<TValue>(query, Aggregate.Maximum, cancellationToken);
        }

        internal static async ValueTask<List<TSource>> ToListCoreAsync<TSource>(Query query, CancellationToken cancellationToken)
        {
            var iter = query.Provider.GetAsyncEnumerator<TSource>(query, cancellationToken);
            try
            {
                var list = new List<TSource>();
                while (await iter.MoveNextAsync().ConfigureAwait(false))
                {
                    list.Add(iter.Current);
                }
                return list;
            }
            finally
            {
                await iter.DisposeAsync().ConfigureAwait(false);
            }
        }

        internal static async ValueTask<TSource[]> ToArrayCoreAsync<TSource>(Query query, CancellationToken cancellationToken)
        {
            var iter = query.Provider.GetAsyncEnumerator<TSource>(query, cancellationToken);
            try
            {
                if (!await iter.MoveNextAsync().ConfigureAwait(false))
                    return Array.Empty<TSource>();

                var list = new List<TSource>();
                do
                {
                    list.Add(iter.Current);
                }
                while (await iter.MoveNextAsync().ConfigureAwait(false));
                return list.ToArray();
            }
            finally
            {
                await iter.DisposeAsync().ConfigureAwait(false);
            }
        }
    }
}
