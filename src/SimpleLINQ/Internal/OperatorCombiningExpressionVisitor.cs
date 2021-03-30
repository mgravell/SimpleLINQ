using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace SimpleLINQ.Internal
{
    internal class OperatorCombiningExpressionVisitor : ExpressionVisitor
    {
        private static readonly OperatorCombiningExpressionVisitor s_Instance = new(); // has no state
        [return: NotNullIfNotNull("expression")]
        internal static Expression? Simplify(Expression? expression)
            => expression is null ? null : s_Instance.Visit(expression);

        public override Expression? Visit(Expression? node) => node switch {

            null => node,
            ConstantExpression => node,
            _ => node.TryGetPureConstantValue(out var value)
                ? Expression.Constant(value, node.Type) : base.Visit(node)
        };


        internal static Expression? Swap(Expression expression, Expression from, Expression? to)
        {
            var visitor = SwapExpressionVisitor.Get();
            try
            {
                visitor.Add(from, to);
                return visitor.Visit(expression);
            }
            finally
            {
                visitor.Reset();
            }
        }
        protected override Expression? VisitInvocation(InvocationExpression node)
        {
            if (node.Expression is LambdaExpression lambda)
            {
                if (lambda.Parameters.Count == 0) return Visit(lambda.Body);

                var visitor = SwapExpressionVisitor.Get();
                try
                {
                    var index = 0;
                    foreach (var p in lambda.Parameters)
                    {
                        visitor.Add(p, Visit(node.Arguments[index++]));
                    }
                    return Visit(visitor.Visit(lambda.Body));
                }
                finally
                {
                    visitor.Reset();
                }
            }
            return base.VisitInvocation(node);
        }

        private sealed class SwapExpressionVisitor : ExpressionVisitor
        {
            [ThreadStatic]
            private static SwapExpressionVisitor? s_PerThreadSwapVisitor;
            internal static SwapExpressionVisitor Get()
            {
                var visitor = s_PerThreadSwapVisitor ??= new SwapExpressionVisitor();
                if (visitor._busy) ThrowBusy();
                visitor._busy = true;
                return visitor;

                static void ThrowBusy() => throw new InvalidOperationException("The thread-bound visitor is already busy! re-entrancy?");
            }
            private bool _busy;
            private readonly Dictionary<Expression, Expression?> _map = new();
            internal void Reset()
            {
                _map.Clear();
                _busy = false;
            }
            internal void Add(Expression from, Expression? to)
                => _map.Add(from, to);
            public override Expression? Visit(Expression? node)
                => node is object && _map.TryGetValue(node, out var result) ? result : base.Visit(node);
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            if (node.TryGetPureConstantValue(out var value))
                return Expression.Constant(value, node.Type);

            Expression? l = Visit(node.Left), r = Visit(node.Right);

            if (node.Method is null && node.Conversion is null)
            {
                static bool TryCombine(BinaryExpression root, Expression? left, Expression? right, [NotNullWhen(true)] out Expression? result)
                {
                    if (left is ConstantExpression ce && right is BinaryExpression { Method: null, Conversion: null } br && br.NodeType == root.NodeType)
                    {
                        var x = ce.Value;
                        if (br.Left.TryGetPureConstantValue(out var y) && ExpressionUtils.TryExecuteBinary(root.NodeType, x, y, out var z))
                        {
                            result = Expression.MakeBinary(root.NodeType, br.Right, Expression.Constant(z, left.Type), root.IsLiftedToNull, root.Method, root.Conversion);
                            return true;
                        }
                        if (br.Right.TryGetPureConstantValue(out y) && ExpressionUtils.TryExecuteBinary(root.NodeType, x, y, out z))
                        {
                            result = Expression.MakeBinary(root.NodeType, br.Left, Expression.Constant(z, left.Type), root.IsLiftedToNull, root.Method, root.Conversion);
                            return true;
                        }
                    }
                    result = null;
                    return false;
                }
                switch (node.NodeType)
                {
                    // stick to operators where we can freely move the operands around
                    case ExpressionType.Multiply:
                    case ExpressionType.MultiplyChecked:
                    case ExpressionType.Add:
                    case ExpressionType.AddChecked:
                        if (TryCombine(node, l, r, out var result)) return result;
                        if (TryCombine(node, r, l, out result)) return result;
                        break;
                }
            }
            return ReferenceEquals(l, node.Left) && ReferenceEquals(r, node.Right)
                ? node : Expression.MakeBinary(node.NodeType, l, r, node.IsLiftedToNull, node.Method, node.Conversion);
        }
    }
}
