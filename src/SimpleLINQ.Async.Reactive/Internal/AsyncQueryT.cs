using SimpleLINQ.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using static SimpleLINQ.QueryProvider;

namespace SimpleLINQ.Async.Internal
{
    internal sealed class AsyncQuery<T> : DecoratedQuery, IAsyncQueryable<T>, IQueryable<T>, IAsyncQueryProvider
    {
        internal AsyncQuery(Query tail) : base(tail) { }

        IAsyncQueryProvider IAsyncQueryable.Provider => this; // we're going to be our own provider, so we can directly use T
        Expression IAsyncQueryable.Expression => Expression.Constant(this);

        protected override Query WrapCore(Query tail)
        {
            if (tail.ElementType == typeof(T)) return new AsyncQuery<T>(tail);

            // do some harder work; TODO - improve this later, type cache etc
            return (Query)AsyncTypeHelper.CreateAsyncQueryTemplate.MakeGenericMethod(tail.ElementType).Invoke(null, new object[] { tail })!;
        }

        IAsyncEnumerator<T> IAsyncEnumerable<T>.GetAsyncEnumerator(CancellationToken cancellationToken)
            => Provider.GetAsyncEnumerator<T>(Tail, cancellationToken);

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
            => Provider.GetEnumerator<T>(Tail);

        IAsyncQueryable<TElement> IAsyncQueryProvider.CreateQuery<TElement>(Expression expression)
        {
            // defer all the logic here to the tail, and re-wrap
            IQueryProvider provider = Tail.Provider;
            return (IAsyncQueryable<TElement>)Wrap((Query)provider.CreateQuery<TElement>(expression));
        }

        ValueTask<TResult> IAsyncQueryProvider.ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
        {
            if (TryResolveAggregate(expression, out var query, out var aggregate))
            {
                if (ReferenceEquals(query, this)) query = Tail; // skip a level of indirection when possible
                return query.Provider.ExecuteAggregateAsync<TResult>(query, aggregate, cancellationToken);
            }

            // RX doesn't provide default implementations for ToListAsync/ToArrayAsync - we need to do that ourselves
            if (IsFromQueryable(expression, out var method, out var args, out query, out var argCount))
            {
                if (ReferenceEquals(query, this)) query = Tail; // skip a level of indirection when possible
                switch (method.Name)
                {
                    case nameof(AsyncQueryable.ToListAsync) when argCount == 0 && typeof(TResult) == typeof(List<T>):
                        if (query.Take == 0) return CoerceAsync<List<T>, TResult>(new List<T>()); // trivial
                        return Coerce<ValueTask<List<T>>, ValueTask<TResult>>(QueryableAsyncExtensions.ToListCoreAsync<T>(query, cancellationToken));
                    case nameof(AsyncQueryable.ToArrayAsync) when argCount == 0 && typeof(TResult) == typeof(T[]):
                        if (query.Take == 0) return CoerceAsync<T[], TResult>(Array.Empty<T>()); // trivial
                        return Coerce<ValueTask<T[]>, ValueTask<TResult>>(QueryableAsyncExtensions.ToArrayCoreAsync<T>(query, cancellationToken));
                }
            }
            ThrowNotSupported(expression);
            return default!;
        }
    }
}
