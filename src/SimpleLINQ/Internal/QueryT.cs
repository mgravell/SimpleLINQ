using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;

namespace SimpleLINQ.Internal
{
    internal class Query<T> : Query, IQueryable<T>, IAsyncEnumerable<T>
    {
        private readonly long _skip, _take;
        private readonly Expression<Func<T, bool>>? _predicate;
        private readonly QueryProvider _provider;
        private readonly object? _providerState;
        
        public override long Skip => _skip;
        public override long Take => _take;
        internal override object? ProviderState => _providerState;
        internal override QueryProvider Provider => _provider;
        public override LambdaExpression? Predicate => _predicate;

        public override Type ElementType => typeof(T);

        public override Type OriginType => typeof(T);

        internal override Query ApplyWhere(LambdaExpression predicate)
        {
            if (predicate is null) return this;
            if (predicate is not Expression<Func<T, bool>> typed)
                throw new ArgumentException("The predicate being applied is not of the correct type", nameof(predicate));

            if (_take == 0) return this; // can't restrict any more
            if (_skip > 0 || _take >= 0) throw new InvalidOperationException("Filters ('Where') cannot be added after row limits ('Skip'/'Take') have been applied");

            typed = AndAlso(_predicate, typed);
            if (ReferenceEquals(predicate, _predicate)) return this;

            return Wrap(_provider, _providerState, typed, _skip, _take);
        }

        internal override Query ApplySkip(long skip)
        {
            if (skip < 0) throw new ArgumentNullException(nameof(skip));
            if (skip == 0 || _take == 0) return this;

            var take = _take;
            if (take >= 0) // has an existing limit
            {
                // reduce (don't scroll) the window; i.e. "skip 3, take 10, skip 2"
                // becomes "skip 5, take 8"
                take -= skip;
                if (take < 0) take = 0;
            }
            return Wrap(_provider, _providerState, _predicate, _skip + skip, take);
        }

        internal override Query ApplyTake(long take)
        {
            if (take < 0) throw new ArgumentNullException(nameof(take));
            if (_take >= 0 && _take <= take) return this;
            return Wrap(_provider, _providerState, _predicate, _skip, take);
        }

        internal override Query ApplyReverse()
        {
            if (_take == 0) return this; // no change
            if (_skip > 0 || _take >= 0) throw new InvalidOperationException("Reverse cannot be applied after row limits ('Skip'/'Take') have been applied");
            if (this is not OrderedQuery<T> ordered) throw new InvalidOperationException("Reverse cannot be applied unless an explicit order has been applied");
            return new OrderedQuery<T>(
                ordered._provider, ordered._providerState, ordered._predicate, ordered._skip, ordered._take, ordered.Tail, !ordered.Reversed);
        }

        internal Query(QueryProvider provider, object? providerState) : this(provider, providerState, null, 0, -1) { }

        protected virtual Query<T> Wrap(QueryProvider provider, object? providerState, Expression<Func<T, bool>>? predicate, long skip, long take)
            => new(provider, providerState, predicate, skip, take);

        internal Query(QueryProvider provider, object? providerState, Expression<Func<T, bool>>? predicate, long skip, long take)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _providerState = providerState;
            Debug.Assert(providerState is not QueryProvider, "Provider state should be the transport, not the provider");
            _predicate = predicate;
            _skip = skip;
            _take = take;
        }

        [return: NotNullIfNotNull("x")]
        [return: NotNullIfNotNull("y")]
        static Expression<Func<T, bool>>? AndAlso(Expression<Func<T, bool>>? x, Expression<Func<T, bool>>? y)
        {
            if (x is null) return y;
            if (y is null) return x;

            // given x: a => foo(a) and y: b => bar(b)
            ParameterExpression xp = x.Parameters.Single(), yp = y.Parameters.Single();

            // note we don't need to use a visitor to normalize, as we need to handle nested invokes *anyway*,
            // so might as well avoid re-writing the entire expression-tree
            var body = ReferenceEquals(xp, yp) ? y.Body : OperatorCombiningExpressionVisitor.Swap(y.Body, yp, xp);

            var rhs = OperatorCombiningExpressionVisitor.Simplify(body); // presume we already trust x to be simplified
            return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(x.Body, rhs), x.Parameters);
        }

        internal override IEnumerator GetUntypedEnumerator()
            => _provider.GetEnumerator<T>(this);

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
            => _provider.GetEnumerator<T>(this);

        IAsyncEnumerator<T> IAsyncEnumerable<T>.GetAsyncEnumerator(CancellationToken cancellationToken)
            => _provider.GetAsyncEnumerator<T>(this, cancellationToken);


        internal override Query ApplyOrderBy(LambdaExpression expression, bool newGroup, bool ascending)
        {
            if (_take == 0 && OrderCount != 0) return this; // taking no rows, and already an ordered query; that'll do
            if (_skip > 0 || _take >= 0) throw new InvalidOperationException("Ordering cannot be applied after row limits ('Skip'/'Take') have been applied");
            var selfOrdered = this as OrderedQuery<T>;
            bool isReversed = selfOrdered?.Reversed ?? false;
            if (isReversed)
            {   // since the entire order-by is being reversed, and we want this to be "right", we need to double-reverse it
                ascending = !ascending;
            }
            var order = new OrderNode(expression.Simplify(), ascending, newGroup, selfOrdered?.Tail);
            return new OrderedQuery<T>(_provider, _providerState, _predicate, _skip, _take, order, isReversed);
        }

        internal override Query ApplySelect(LambdaExpression projection)
            => TypeHelper.Select(projection, this);

        internal override Query ApplyDistinct(bool distinct)
            => throw new NotSupportedException("Distinct can only be applied to a projection ('Select')");

        public override bool Distinct => false;
    }
}
