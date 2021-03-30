using System;
using System.Linq.Expressions;

namespace SimpleLINQ.Internal
{
    internal sealed class OrderNode
    {
        /// <inheritdoc/>
        public override string ToString() => Expression?.ToString() ?? "";
        internal OrderNode(LambdaExpression expression, bool ascending, bool newGroup, OrderNode? head)
        {
            Expression = expression;
            Head = head;
            int count = (((head?.Count) ?? 0) + 1) << 2;
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count)); // use 2 low bits for flags
            if (ascending) count |= 0b01;
            if (newGroup) count |= 0b10;
            _countAndFlags = count;

        }
        internal LambdaExpression Expression { get; }
        internal OrderNode? Head { get; }
        private readonly int _countAndFlags;
        internal int Count => _countAndFlags >> 2;
        internal bool Ascending => (_countAndFlags & 0b01) != 0;
        internal bool NewGroup => (_countAndFlags & 0b10) != 0;
    }
}
