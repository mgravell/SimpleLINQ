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

            var data = FilterOrderAndProject<TElement>(query);
            ConsiderHandled(QueryTerms.Where | QueryTerms.OrderBy | QueryTerms.Select);

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
            
            if (terms != 0)
                throw new InvalidOperationException($"Query generator failed to consider: {terms}");

            return data.GetEnumerator();
        }
        private IEnumerable<TElement> FilterOrderAndProject<TElement>(Query query)
        {
            
            var projection = ((Expression<Func<T, TElement>>)query.Projection)?.Compile(preferInterpretation: true);

            IEnumerable<T> values = _values;
            var filter = ((Expression<Func<T, bool>>)query.Predicate)?.Compile(preferInterpretation: true);
            if (filter is not null)
            {
                values = Enumerable.Where(values, filter);
            }
            if (query.OrderCount != 0)
            {
                // performance doesn't matter here; we'll be fairly lazy
                var arr = new OrderClause[query.OrderCount];
                query.CopyOrderTo(arr);
                dynamic d = values;
                for (int i = 0; i < arr.Length; i++)
                {
                    dynamic expr = arr[i].Expression.Compile();
                    d = i == 0
                        ? arr[0].Ascending ? Enumerable.OrderBy(d, expr) : Enumerable.OrderByDescending(d, expr)
                        : arr[i].Ascending ? Enumerable.ThenBy(d, expr) : Enumerable.ThenByDescending(d, expr);
                }
                values = d;
            }
            foreach (var item in values)
            {
                yield return projection is null ? (TElement)(object)item : projection(item);
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
