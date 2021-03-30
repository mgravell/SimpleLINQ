using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace SimpleLINQ.Internal
{
    internal abstract class DecoratedQuery : Query
    {
        private readonly Query _tail;
        protected Query Tail => _tail;
        internal DecoratedQuery(Query tail)
            => _tail = tail;

        [return:NotNullIfNotNull("tail")]
        protected Query? Wrap(Query? tail)
        {
            // nul
            if (tail is null) return null;

            // identity operation?
            if (ReferenceEquals(_tail, tail)) return this;

            // defer to implementation
            return WrapCore(tail);
        }

        protected abstract Query WrapCore(Query tail);

        internal override IEnumerator GetUntypedEnumerator()
            => _tail.GetUntypedEnumerator();

        internal override Query ApplyOrderBy(LambdaExpression expression, bool newGroup, bool ascending)
            => Wrap(_tail.ApplyOrderBy(expression, newGroup, ascending));

        internal override Query ApplyReverse()
            => Wrap(_tail.ApplyReverse());

        internal override Query ApplySkip(long skip)
            => Wrap(_tail.ApplySkip(skip));

        internal override Query ApplyTake(long take)
            => Wrap(_tail.ApplyTake(take));

        internal override Query ApplyWhere(LambdaExpression predicate)
            => Wrap(_tail.ApplyWhere(predicate));

        internal override Query ApplySelect(LambdaExpression lambda)
            => Wrap(_tail.ApplySelect(lambda));

        internal override Query ApplyDistinct(bool distinct)
            => Wrap(_tail.ApplyDistinct(distinct));

        public override bool Distinct => _tail.Distinct;

        public override int OrderCount => _tail.OrderCount;
        public override void CopyOrderTo(Span<OrderClause> order)
            => _tail.CopyOrderTo(order);

        public override Type ElementType => _tail.ElementType;
        public override Type OriginType => _tail.OriginType;
        public override LambdaExpression? Predicate => _tail.Predicate;
        public override LambdaExpression? Projection => _tail.Projection;
        public override QueryProvider Provider => _tail.Provider;
        public override object? ProviderState => _tail.ProviderState;
        public override long Skip => _tail.Skip;
        public override long Take => _tail.Take;
    }
}
