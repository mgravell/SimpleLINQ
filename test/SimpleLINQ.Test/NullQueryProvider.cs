using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;

namespace SimpleLINQ.Test
{
    class NullQueryProvider<T> : QueryProvider
    {
        private readonly IList<T> _values;
        private NullQueryProvider() : this(Array.Empty<T>()) { }
        internal IQueryable<TQuery> CreateQuery<TQuery>() => CreateQuery<TQuery>(null);

        public NullQueryProvider(params T[] values)
            => _values = values ?? Array.Empty<T>();
        public NullQueryProvider(IList<T> values)
            => _values = values ?? Array.Empty<T>();
        public static NullQueryProvider<T> Default { get; } = new NullQueryProvider<T>();

        internal override bool AllowExpensiveAggregates => true;

        protected internal override IEnumerator<TElement> GetEnumerator<TElement>(Query query)
        {
            var terms = query.ActiveTerms;
            void ConsiderHandled(QueryTerms handled)
                => terms &= ~handled;

            var data = FilterAndProject<TElement>(query);
            ConsiderHandled(QueryTerms.Where | QueryTerms.Select);

            if (query.Distinct)
            {
                data = data.Distinct();
                ConsiderHandled(QueryTerms.Distinct);
            }

            if (query.Skip != 0)
            {
                data = data.Skip(checked((int)query.Skip));
                ConsiderHandled(QueryTerms.Skip);
            }

            if (query.Take != 0)
            {
                data = data.Take(checked((int)query.Take));
                ConsiderHandled(QueryTerms.Take);
            }
            
            if (query.OrderCount != 0)
            {
                var arr = new OrderClause[query.OrderCount];
                query.CopyOrderTo(arr);
                dynamic d = data;
                d = arr[0].Ascending ? Enumerable.OrderBy(d, arr[0].Expression) : Enumerable.OrderByDescending(d, arr[0].Expression);
                for (int i = 1; i < arr.Length; i++)
                {
                    d = arr[i].Ascending ? Enumerable.ThenBy(d, arr[i].Expression) : Enumerable.ThenByDescending(d, arr[i].Expression);
                }
                data = d;
                ConsiderHandled(QueryTerms.OrderBy);
            }
            if (terms != 0)
                throw new InvalidOperationException($"Query generator failed to consider: {terms}");

            return data.GetEnumerator();
        }
        private IEnumerable<TElement> FilterAndProject<TElement>(Query query)
        {
            var filter = ((Expression<Func<T, bool>>)query.Predicate)?.Compile(preferInterpretation: true);
            var projection = ((Expression<Func<T, TElement>>)query.Projection)?.Compile(preferInterpretation: true);

            foreach (var item in _values)
            {
                if (filter is null || filter(item))
                {
                    yield return projection is null ? (TElement)(object)item : projection(item);
                }
            }
        }
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        protected internal async override IAsyncEnumerator<TElement> GetAsyncEnumerator<TElement>(Query query, CancellationToken cancellationToken)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var iter = GetEnumerator<TElement>(query);
            while (iter.MoveNext())
            {
                yield return iter.Current;
                cancellationToken.ThrowIfCancellationRequested();
            }
        }
    }
}
