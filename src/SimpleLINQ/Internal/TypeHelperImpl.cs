using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using static SimpleLINQ.QueryProvider;

namespace SimpleLINQ.Internal
{
    /// <summary>
    /// Allows the caller to switch from a type known only as a <c>Type</c>, to a generic <c>T</c>
    /// </summary>
    internal abstract class TypeHelper
    {

        // primary static APIs
        internal static object Execute(QueryProvider provider, Expression expression)
            => GetHelper(expression.Type).ExecuteCore(provider, expression);
        internal static TResult ExecuteAggregate<TResult>(Query query, Aggregate aggregate)
            => GetHelper(query.ElementType).ExecuteAggregateCore<TResult>(query, aggregate);
        internal static ValueTask<TResult> ExecuteAggregateAsync<TResult>(Query query, Aggregate aggregate, CancellationToken cancellationToken)
            => GetHelper(query.ElementType).ExecuteAggregateCoreAsync<TResult>(query, aggregate, cancellationToken);
        internal static Task ExecuteAggregateTaskAsync<TResult>(Query query, Aggregate aggregate, CancellationToken cancellationToken)
            => GetHelper(GetGenericArgs(typeof(TResult))[0]).ExecuteAggregateTaskCoreAsync(query, aggregate, cancellationToken);

        internal static Query Select(LambdaExpression projection, Query tail)
        {
            // detect x => x - that isn't really a projection
            if (projection is null || projection.IsIdentity()) return tail;

            // check for tail queries that disallow projections
            if (tail.Projection is not null) throw new NotSupportedException("Only single projections are supported");
            if (tail.Take != 0) // we can get away with a few more things in the trivial empty case
            {
                if (tail.Distinct) throw new NotSupportedException("Projections cannot be constructed from queries marked as distinct");
                if (tail.Skip != 0 || tail.Take > 0) throw new NotSupportedException("Projections cannot be constructed from queries with skip/take applied");
            }

            if (!TryGetResultType(projection, tail.ElementType, out var resultType))
            {
                throw new InvalidOperationException("Unable to resolve result type");
            }
            return GetHelper(resultType).SelectCore(projection, tail);

            static bool TryGetResultType(LambdaExpression projection, Type elementType, [NotNullWhen(true)] out Type? resultType)
            {
                if (projection.Parameters.Count == 1 && projection.Parameters[0].Type == elementType)
                {
                    resultType = projection.Body.Type;
                    return true;
                }
                resultType = default;
                return false;
            }
        }

        // instance APIs
        protected abstract Query SelectCore(LambdaExpression projection, Query tail);
        protected abstract TResult ExecuteAggregateCore<TResult>(Query query, Aggregate aggregate);
        protected abstract ValueTask<TResult> ExecuteAggregateCoreAsync<TResult>(Query query, Aggregate aggregate, CancellationToken cancellationToken);
        protected abstract object ExecuteCore(QueryProvider provider, Expression expression);
        protected abstract Task ExecuteAggregateTaskCoreAsync(Query query, Aggregate aggregate, CancellationToken cancellationToken);

        // internal machinery
        private static readonly ConcurrentDictionary<Type, TypeHelper> s_knownTypes = new();
        private static readonly ConcurrentDictionary<Type, Type[]> s_genericArgs = new();

        private static ReadOnlySpan<Type> GetGenericArgs(Type type)
        {
            if (!s_genericArgs.TryGetValue(type, out var t))
            {
                s_genericArgs[type] = t = type.IsGenericType ? type.GetGenericArguments() : Type.EmptyTypes;
            }
            return t;
        }

        private static TypeHelper GetHelper(Type type)
        {
            if (!s_knownTypes.TryGetValue(type, out var helper))
            {
                s_knownTypes[type] = helper = (TypeHelper)Activator.CreateInstance(typeof(TypeHelperImpl<>).MakeGenericType(type))!;
            }
            return helper;
        }

        // generic sub-type to provider the type switch
        internal sealed class TypeHelperImpl<T> : TypeHelper
        {
            protected override Query SelectCore(LambdaExpression projection, Query tail)
                => new ProjectedQuery<T>(tail, projection, false);

            protected override TResult ExecuteAggregateCore<TResult>(Query query, Aggregate aggregate)
                => query.Provider.ExecuteAggregate<T, TResult>(query, aggregate);

            protected override object ExecuteCore(QueryProvider provider, Expression expression)
                => provider.Execute<T>(expression)!;

            protected override ValueTask<TResult> ExecuteAggregateCoreAsync<TResult>(Query query, Aggregate aggregate, CancellationToken cancellationToken)
                => query.Provider.ExecuteAggregateAsync<T, TResult>(query, aggregate, cancellationToken);

            protected override Task ExecuteAggregateTaskCoreAsync(Query query, Aggregate aggregate, CancellationToken cancellationToken)
                => query.Provider.ExecuteAggregateAsync<T>(query, aggregate, cancellationToken).AsTask();
        }
    }
}
