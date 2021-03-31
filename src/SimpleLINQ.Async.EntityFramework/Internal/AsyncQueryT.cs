using Microsoft.EntityFrameworkCore.Query;
using SimpleLINQ.Internal;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using static SimpleLINQ.QueryProvider;

namespace SimpleLINQ.Async.Internal
{
    internal sealed class AsyncQuery<T> : DecoratedQuery, IQueryable<T>, IAsyncQueryProvider, IAsyncEnumerable<T>, IQueryable
    {
        internal AsyncQuery(Query tail) : base(tail) { }

        protected override Query WrapCore(Query tail)
        {
            if (tail.ElementType == typeof(T)) return new AsyncQuery<T>(tail);

            // do some harder work; TODO - improve this later, type cache etc
            return (Query)AsyncTypeHelper.CreateAsyncQueryTemplate.MakeGenericMethod(tail.ElementType).Invoke(null, new object[] { tail })!;
        }

        IQueryProvider IQueryable.Provider => this; // we're going to be our own provider, so we can directly use T
        IQueryable IQueryProvider.CreateQuery(Expression expression)
        {
            // defer all the logic here to the tail, and re-wrap
            IQueryProvider provider = Tail.Provider;
            return Wrap((Query)provider.CreateQuery(expression));
        }

        IQueryable<TElement> IQueryProvider.CreateQuery<TElement>(Expression expression)
        {
            // defer all the logic here to the tail, and re-wrap
            IQueryProvider provider = Tail.Provider;
            return (IQueryable<TElement>)Wrap((Query)provider.CreateQuery<TElement>(expression));
        }

        object IQueryProvider.Execute(Expression expression)
        {
            IQueryProvider provider = Tail.Provider;
            return provider.Execute(expression);
        }

        TResult IQueryProvider.Execute<TResult>(Expression expression)
        {
            IQueryProvider provider = Tail.Provider;
            return provider.Execute<TResult>(expression);
        }

        IAsyncEnumerator<T> IAsyncEnumerable<T>.GetAsyncEnumerator(CancellationToken cancellationToken)
            => Provider.GetAsyncEnumerator<T>(Tail, cancellationToken);

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
            => Provider.GetEnumerator<T>(Tail);

        TResult IAsyncQueryProvider.ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
        {
            if (typeof(TResult).IsGenericType && typeof(TResult).GetGenericTypeDefinition() == typeof(Task<>)
                && TryResolveAggregate(expression, out var query, out var aggregate))
            {
                if (ReferenceEquals(query, this)) query = Tail; // skip a level of indirection when possible

                // since this is Task<blah>, this isn't a box
                return (TResult)(object)TypeHelper.ExecuteAggregateTaskAsync<TResult>(query, aggregate, cancellationToken);
            }
            ThrowNotSupported(expression);
            return default!;
        }
    }
}
