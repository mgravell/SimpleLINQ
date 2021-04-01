using System;
using System.Collections;
using System.Linq;
using System.Linq.Expressions;

namespace SimpleLINQ
{
    /// <summary>
    /// Represents a general purpose simplified LINQ query
    /// </summary>
    public abstract class Query : IQueryable
    {
        internal Query() { } // don't want unknown subclasses, sorry folks

        // primary public API
        /// <summary>
        /// The number of elements to omit at the start of the sequence
        /// </summary>
        public virtual long Skip => 0;
        /// <summary>
        /// The maximum number of elements to yield in the sequence
        /// </summary>
        public virtual long Take => -1;
        /// <summary>
        /// The combined filter expression (if any) in the query
        /// </summary>
        public virtual LambdaExpression? Predicate => null;
        /// <summary>
        /// Opaque provider state (implementation-specific)
        /// </summary>
        internal abstract object? ProviderState { get; }
        /// <summary>
        /// Gets the provider for this query (implementation-specific)
        /// </summary>
        internal abstract QueryProvider Provider { get; }
        /// <summary>
        /// Gets the number of order-by clauses for this query
        /// </summary>
        public virtual int OrderCount => 0;
        /// <summary>
        /// Copies out the order-by clauses for this query
        /// </summary>
        public virtual void CopyOrderTo(Span<OrderClause> order) { }
        /// <summary>
        /// Gets the projection for this query, if one, that maps from <see cref="OriginType"/> to <see cref="ElementType"/>
        /// </summary>
        public virtual LambdaExpression? Projection => null;
        /// <summary>
        /// The underlying source type for this query
        /// </summary>
        public abstract Type OriginType { get; }
        /// <summary>
        /// The resultant element type for this query, after considering <see cref="Projection"/>
        /// </summary>
        public abstract Type ElementType { get; }
        /// <summary>
        /// Whether to exclude duplicate data in this query
        /// </summary>
        public abstract bool Distinct { get; }

        // framework machinery
        IEnumerator IEnumerable.GetEnumerator() => GetUntypedEnumerator();
        Expression IQueryable.Expression => Expression.Constant(this);
        IQueryProvider IQueryable.Provider => Provider;

        /// <summary>
        /// Gets the provider-specific representation of this query
        /// </summary>
        public sealed override string ToString() => Provider?.ToString(this, null) ?? base.ToString() ?? "";
        /// <summary>
        /// Gets the provider-specific representation of this query, when applying the specified aggregate
        /// </summary>
        public string ToString(Aggregate aggregate) => Provider?.ToString(this, aggregate) ?? base.ToString() ?? aggregate.ToString();

        // implementation machinery
        internal abstract IEnumerator GetUntypedEnumerator();
        internal abstract Query ApplySkip(long skip);
        internal abstract Query ApplyTake(long take);
        internal abstract Query ApplyWhere(LambdaExpression predicate);
        internal abstract Query ApplyOrderBy(LambdaExpression expression, bool newGroup, bool ascending);
        internal abstract Query ApplyReverse();
        internal abstract Query ApplySelect(LambdaExpression lambda);
        internal abstract Query ApplyDistinct(bool distinct);

        /// <summary>
        /// Indicates whether this query includes any of the specified terms
        /// </summary>
        public bool HasAny(QueryTerms terms) => (ActiveTerms & terms) != 0;

        /// <summary>
        /// Indicates whether this query includes anything outside of the specified terms
        /// </summary>
        public bool HasAtMost(QueryTerms terms) => (ActiveTerms & ~terms) == 0;

        /// <summary>
        /// Gets the active terms that apply for this query
        /// </summary>
        public QueryTerms ActiveTerms
        {
            get
            {
                var terms = QueryTerms.None;
                if (Skip != 0) terms |= QueryTerms.Skip;
                if (Take >= 0) terms |= QueryTerms.Take;
                if (Predicate is not null) terms |= QueryTerms.Where;
                if (OrderCount != 0) terms |= QueryTerms.OrderBy;
                if (Projection is not null) terms |= QueryTerms.Select;
                if (Distinct) terms |= QueryTerms.Distinct;
                return terms;
            }
        }
    }

    /// <summary>
    /// Describes the operations that might apply to a <see cref="Query"/>
    /// </summary>
    [Flags]
    public enum QueryTerms
    {
        /// <summary>
        /// No terms
        /// </summary>
        None = 0,
        /// <summary>
        /// <see cref="Queryable.Skip{TSource}(IQueryable{TSource}, int)"/> has been applied
        /// </summary>
        Skip = 1 << 0,
        /// <summary>
        /// <see cref="Queryable.Take{TSource}(IQueryable{TSource}, int)"/> has been applied
        /// </summary>
        Take = 1 << 1,
        /// <summary>
        /// <see cref="Queryable.Where{TSource}(IQueryable{TSource}, Expression{Func{TSource, bool}})"/> has been applied
        /// </summary>
        Where = 1 << 2,
        /// <summary>
        /// <see cref="Queryable.OrderBy{TSource, TKey}(IQueryable{TSource}, Expression{Func{TSource, TKey}})"/> has been applied
        /// </summary>
        OrderBy = 1 << 3,
        /// <summary>
        /// <see cref="Queryable.Select{TSource, TResult}(IQueryable{TSource}, Expression{Func{TSource, TResult}})"/> has been applied
        /// </summary>
        Select = 1 << 4,
        /// <summary>
        /// <see cref="Queryable.Distinct{TSource}(IQueryable{TSource})"/> has been applied
        /// </summary>
        Distinct = 1 << 5,
    }


}
