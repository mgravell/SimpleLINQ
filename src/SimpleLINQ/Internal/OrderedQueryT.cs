using System;
using System.Linq;
using System.Linq.Expressions;

namespace SimpleLINQ.Internal
{
    internal sealed class OrderedQuery<T> : Query<T>, IOrderedQueryable<T>
    {
        internal OrderNode Tail { get; }
        internal bool Reversed { get; }
        internal OrderedQuery(QueryProvider provider, object? providerState, Expression<Func<T, bool>>? predicate, long skip, long take, OrderNode order, bool reversed)
            : base(provider, providerState, predicate, skip, take)
        {
            Tail = order ?? throw new ArgumentNullException(nameof(order));
            Reversed = reversed;
        }

        protected override Query<T> Wrap(QueryProvider provider, object? providerState, Expression<Func<T, bool>>? predicate, long skip, long take)
            => new OrderedQuery<T>(provider, providerState, predicate, skip, take, Tail, Reversed);

        public override int OrderCount => Tail.Count;

        public override void CopyOrderTo(Span<OrderClause> order)
        {
            var node = Tail;
            int index = 0, groupStartIndex = 0;
            while (node is object)
            {

                var value = new OrderClause(node.Expression, Reversed ? !node.Ascending : node.Ascending);
                order[index++] = value;
                if (node.NewGroup)
                {
                    Reverse(order, groupStartIndex, index);
                    groupStartIndex = index;
                }
                node = node.Head;
            }
            Reverse(order, groupStartIndex, index);

            static void Reverse(Span<OrderClause> all, int start, int end)
            {
                var length = end - start;
                if (length > 1)
                {
                    all.Slice(start, length).Reverse();
                }
            }
        }
    }
}
