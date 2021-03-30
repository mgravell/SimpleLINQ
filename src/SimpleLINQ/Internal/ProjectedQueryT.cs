using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;

namespace SimpleLINQ.Internal
{
    internal sealed class ProjectedQuery<T> : Query, IQueryable<T>, IAsyncEnumerable<T>
    {
        internal ProjectedQuery(Query tail, LambdaExpression projection, bool distinct)
        {   // note: additional rules in TypeHelper.Select, which is the only way into ProjectedQuery that isn't from itself
            _tail = tail;
            _projection = projection;
            _distinct = distinct;
        }

        private readonly LambdaExpression _projection;
        private readonly Query _tail;
        private readonly bool _distinct;

        public override Type ElementType => typeof(T);
        public override bool Distinct => _distinct;

        Expression IQueryable.Expression => Expression.Constant(this);

        public override QueryProvider Provider => _tail.Provider;

        public override long Skip => _tail.Skip;

        public override long Take => _tail.Take;

        public override LambdaExpression? Predicate => _tail.Predicate;

        public override object? ProviderState => _tail.ProviderState;

        public override int OrderCount => _tail.OrderCount;

        public override Type OriginType => _tail.OriginType;

        public override LambdaExpression Projection => _projection;


        IEnumerator<T> IEnumerable<T>.GetEnumerator()
            => Provider.GetEnumerator<T>(this);

        IEnumerator IEnumerable.GetEnumerator()
            => Provider.GetEnumerator<T>(this);

        internal override IEnumerator GetUntypedEnumerator()
            => Provider.GetEnumerator<T>(this);

        internal override Query ApplySkip(long skip) => Wrap(_tail.ApplySkip(skip));

        internal override Query ApplyTake(long take) => Wrap(_tail.ApplyTake(take));

        ProjectedQuery<T> Wrap(Query tail) => ReferenceEquals(tail, _tail) ? this : new ProjectedQuery<T>(tail, _projection, _distinct);

        internal override Query ApplyWhere(LambdaExpression predicate)
            => Wrap(_tail.ApplyWhere(ExpressionUtils.Merge(_projection, predicate)));

        internal override Query ApplyOrderBy(LambdaExpression expression, bool newGroup, bool ascending)
        {
            if (!ReferenceEquals(expression, _projection))
            {   // ^^^ sometimes (Min/Max fallbacks) we use a cheeky root-based aggregate - doesn't need merge
                expression = ExpressionUtils.Merge(_projection, expression);
            }
            return Wrap(_tail.ApplyOrderBy(expression, newGroup, ascending));
        }

        internal override Query ApplyReverse()
            => Wrap(_tail.ApplyReverse());

        internal override Query ApplySelect(LambdaExpression lambda)
        {
            if (lambda is null || lambda.IsIdentity()) return this;
            if (_distinct) throw new NotSupportedException("Additional projections cannot be added after distinct");
            return TypeHelper.Select(Projection.Merge(lambda), _tail);
        }

        internal override Query ApplyDistinct(bool distinct)
        {
            if ((distinct == Distinct) || Take == 0) return this;
            if (distinct && (Skip != 0 || Take > 0)) throw new NotSupportedException("Distinct cannot be applied after Skip/Take operations");
            return new ProjectedQuery<T>(_tail, _projection, distinct);
        }

        public override void CopyOrderTo(Span<OrderClause> order)
            => _tail.CopyOrderTo(order);
        IAsyncEnumerator<T> IAsyncEnumerable<T>.GetAsyncEnumerator(CancellationToken cancellationToken)
            => Provider.GetAsyncEnumerator<T>(this, cancellationToken);
    }
}
