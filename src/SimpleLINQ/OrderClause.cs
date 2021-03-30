using System.Linq.Expressions;

namespace SimpleLINQ
{
    /// <summary>
    /// Describes a single order-by condition
    /// </summary>
    public readonly struct OrderClause
    {
        /// <inheritdoc/>
        public override string ToString() => Expression?.ToString() ?? "";
        /// <summary>
        /// Whether the considiton is ascending (<c>true</c>) or descending (<c>false</c>)
        /// </summary>
        public bool Ascending { get; }
        /// <summary>
        /// The expression that describes the order clause
        /// </summary>
        public LambdaExpression Expression { get; }
        internal OrderClause(LambdaExpression expression, bool ascending)
        {
            Expression = expression;
            Ascending = ascending;
        }
    }
}
