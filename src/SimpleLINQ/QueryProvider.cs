using SimpleLINQ.Internal;
using SimpleLINQ.Transports;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleLINQ
{
    /// <summary>
    /// Base type for query-provider implementations; the provider is responsible for converting a <see cref="Query"/> into a command that
    /// can be executed against a <see cref="Transport"/>
    /// </summary>
    public abstract partial class QueryProvider : IQueryProvider
    {
        /// <summary>
        /// Create a new query against this provider, optionally including state
        /// </summary>
        protected IQueryable<T> CreateQuery<T>(object? state = null) => new Query<T>(this, state);
        IQueryable IQueryProvider.CreateQuery(Expression expression) => CreateQuery(expression);

        /// <summary>
        /// Gets the state object associated with a query
        /// </summary>
        protected object? GetState(Query query)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (!ReferenceEquals(query.Provider, this)) throw new ArgumentException("Incorrect provider; the state object is only available to the provider that created the query", nameof(query));
            return query.ProviderState;
        }

        private Query CreateQuery(Expression expression)
        {
            if (IsFromQueryable(expression, out var method, out var args, out var query, out var argCount))
            {
                switch (method.Name)
                {
                    case nameof(Queryable.Where) when argCount == 1 && TryGetLambda(args[1], out var lambda):
                        return query.ApplyWhere(lambda);
                    case nameof(Queryable.Skip) when argCount == 1
                                && (args[1] as ConstantExpression)?.Value is int skip:
                        return query.ApplySkip(skip);
                    case nameof(Queryable.Take) when argCount == 1
                                && (args[1] as ConstantExpression)?.Value is int take:
                        return query.ApplyTake(take);
                    case nameof(Queryable.OrderBy) when argCount == 1 && TryGetLambda(args[1], out var lambda):
                        return query.ApplyOrderBy(lambda, true, true);
                    case nameof(Queryable.OrderByDescending) when argCount == 1 && TryGetLambda(args[1], out var lambda):
                        return query.ApplyOrderBy(lambda, true, false);
                    case nameof(Queryable.ThenBy) when argCount == 1 && TryGetLambda(args[1], out var lambda):
                        return query.ApplyOrderBy(lambda, false, true);
                    case nameof(Queryable.ThenByDescending) when argCount == 1 && TryGetLambda(args[1], out var lambda):
                        return query.ApplyOrderBy(lambda, false, false);
                    case nameof(Queryable.Reverse) when argCount == 0:
                        return query.ApplyReverse();
                    case nameof(Queryable.Select) when argCount == 1 && TryGetLambda(args[1], out var lambda)
                        && lambda.Parameters.Count == 1 && lambda.Parameters[0].Type == query.ElementType:
                        return query.ApplySelect(lambda);
                    case nameof(Queryable.Distinct) when argCount == 0:
                        return query.ApplyDistinct(true);
                }
            }
            ThrowNotSupported(expression);
            return default!;
        }

        internal static bool TryGetLambda(Expression expression, [NotNullWhen(true)] out LambdaExpression? value)
        {
            switch (expression)
            {
                case LambdaExpression lambda:
                    value = lambda;
                    return true;
                case UnaryExpression ue when ue.NodeType == ExpressionType.Quote
                     && ue.Operand is LambdaExpression lambda:
                    value = lambda;
                    return true;
            }
            value = default;
            return false;
        }

        IQueryable<TElement> IQueryProvider.CreateQuery<TElement>(Expression expression)
        {
            var untyped = CreateQuery(expression);
            if (untyped is IQueryable<TElement> typed) return typed;
            if (untyped is object) ThrowTypeFail();
            return null!;

            static void ThrowTypeFail()
            {
                throw new InvalidOperationException("The result from " + nameof(CreateQuery) + " was not of the expected type");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [SuppressMessage("Style", "IDE0066:Convert switch statement to expression", Justification = "Readability")]
        internal static void ThrowNotSupported(Expression expression, [CallerMemberName] string? caller = null)
        {
            switch (expression.NodeType)
            {
                case ExpressionType.Call when expression is MethodCallExpression mce
                    && mce.Method is MethodInfo method:
                    throw new NotSupportedException($"Unhandled '{expression.NodeType}' ('{method.Name}') expression to '{caller}': '{method}' on '{method.DeclaringType?.FullName}'");
            }
            throw new NotSupportedException($"Unhandled '{expression.NodeType}' expression to '{caller}'");
        }

        internal static bool IsFromQueryable(Expression expression, [NotNullWhen(true)] out MethodInfo? method, [NotNullWhen(true)] out ReadOnlyCollection<Expression>? args, [NotNullWhen(true)] out Query? query, out int argCount)
        {
            switch (expression.NodeType)
            {
                case ExpressionType.Call when expression is MethodCallExpression mce
                    && (args = mce.Arguments).Count > 0
                    && mce.Method is MethodInfo mmethod:

                    if ((mmethod.DeclaringType == typeof(Queryable)
                    || mmethod.DeclaringType?.FullName == "System.Linq.AsyncQueryable"
                        ) && (args[0] as ConstantExpression)?.Value is Query oorigin)
                    {
                        method = mmethod;
                        query = oorigin;
                        argCount = args.Count - 1; // don't count the "this" source
                        if (argCount > 0 && args[argCount].Type == typeof(CancellationToken))
                            argCount--; // don't count the cancellation-token
                        return true;
                    }
                    break;
            }
            args = default;
            method = default;
            query = default;
            argCount = default;
            return false;
        }

        internal string ToString(Query query, Aggregate? aggregate)
        {
            if (query is not null)
            {
                var result = TryRenderCore(query, aggregate);
                if (result is not null) return result;
            }
            var name = GetType().FullName ?? "";
            return aggregate is null ? name : (aggregate.ToString() + ":" + name);
        }

        private string? TryRenderCore(Query query, Aggregate? aggregate)
        {
            var result = TryRender(query, aggregate);
            if (!string.IsNullOrEmpty(result)) return result;

            if (aggregate != null)
            {
                var a = aggregate.GetValueOrDefault();
                var q = query;
                if (HasFallbackAggregate(ref q, ref a))
                {
                    result = TryRenderCore(q, a);
                    if (!string.IsNullOrEmpty(result)) return result;
                }

                switch (aggregate.GetValueOrDefault())
                {
                    case Aggregate.NotAny:
                        result = TryRender(query, Aggregate.Any);
                        if (!string.IsNullOrEmpty(result)) return result;
                        break;
                    case Aggregate.Any:
                    case Aggregate.First:
                    case Aggregate.FirstOrDefault:
                        result = TryRender(query.ApplyTake(1).RemoveDistinctIfNoSkip(), null);
                        if (!string.IsNullOrEmpty(result)) return result;
                        break;
                    case Aggregate.Single:
                    case Aggregate.SingleOrDefault:
                        result = TryRender(query.ApplyTake(2), null);
                        if (!string.IsNullOrEmpty(result)) return result;
                        break;
                    case Aggregate.Count when AllowExpensiveAggregates:
                        result = TryRender(query, null);
                        if (!string.IsNullOrEmpty(result)) return result;
                        break;

                }
            }
            return null;
        }

        /// <summary>
        /// Attempt to get a string reprentation of the given <see cref="Query"/>, optionally considering an <see cref="Aggregate"/>
        /// </summary>
        protected virtual string? TryRender(Query query, Aggregate? aggregate)
            => default;

        object IQueryProvider.Execute(Expression expression)
            => TypeHelper.Execute(this, expression);

        TResult IQueryProvider.Execute<TResult>(Expression expression)
            => Execute<TResult>(expression);

        /// <summary>
        /// Perform a synchronous <see cref="Aggregate"/> over a <see cref="Query"/>, returning a single value
        /// </summary>
        protected internal virtual TResult ExecuteAggregate<TResult>(Query query, Aggregate aggregate)
        {
            switch (aggregate)
            {
                case Aggregate.NotAny when typeof(TResult) == typeof(bool):
                    if (query.Take == 0) return Coerce<bool, TResult>(true);
                    var notAny = !ExecuteAggregate<bool>(query, Aggregate.Any);
                    return Unsafe.As<bool, TResult>(ref notAny);
                case Aggregate.Sum:
                case Aggregate.Count:
                    if (query.Take == 0 && (typeof(TResult) == typeof(int) || typeof(TResult) == typeof(long)))
                        return default!;
                    break;
                case Aggregate.Any when typeof(TResult) == typeof(bool) && query.Take == 0:
                    return default!;
            }
            if (HasFallbackAggregate(ref query, ref aggregate))
                return ExecuteAggregate<TResult>(query, aggregate);
            return TypeHelper.ExecuteAggregate<TResult>(query, aggregate);
        }

        /// <summary>
        /// Perform an asynchronous <see cref="Aggregate"/> over a <see cref="Query"/>, returning a single value
        /// </summary>
        protected internal virtual ValueTask<TResult> ExecuteAggregateAsync<TResult>(Query query, Aggregate aggregate, CancellationToken cancellationToken)
        {

            switch (aggregate)
            {
                case Aggregate.NotAny when typeof(TResult) == typeof(bool):
                    if (query.Take == 0) return CoerceAsync<bool, TResult>(true);
                    var notAny = NotAsync(ExecuteAggregateAsync<bool>(query, Aggregate.Any, cancellationToken));
                    return Unsafe.As<ValueTask<bool>, ValueTask<TResult>>(ref notAny);
                case Aggregate.Sum:
                case Aggregate.Count:
                    if (query.Take == 0 && (typeof(TResult) == typeof(int) || typeof(TResult) == typeof(long)))
                        return default;
                    break;
                case Aggregate.Any when typeof(TResult) == typeof(bool) && query.Take == 0:
                    return default;
            }
            if (HasFallbackAggregate(ref query, ref aggregate))
                return ExecuteAggregateAsync<TResult>(query, aggregate, cancellationToken);
            return TypeHelper.ExecuteAggregateAsync<TResult>(query, aggregate, cancellationToken);
        }

        private static bool HasFallbackAggregate(ref Query query, ref Aggregate aggregate)
        {
            switch (aggregate)
            {
                case Aggregate.Minimum when query.Projection is not null:
                    query = query.ApplyOrderBy(query.Projection, true, true).RemoveDistinctIfNoSkip();
                    aggregate = Aggregate.First;
                    return true;
                case Aggregate.Maximum when query.Projection is not null:
                    query = query.ApplyOrderBy(query.Projection, true, false).RemoveDistinctIfNoSkip();
                    aggregate = Aggregate.First;
                    return true;
            }
            return false;
        }

        internal virtual bool AllowExpensiveAggregates => false;

        static ValueTask<bool> NotAsync(ValueTask<bool> pending)
        {
            return pending.IsCompletedSuccessfully ? new(!pending.Result) : Awaited(pending);

            static async ValueTask<bool> Awaited(ValueTask<bool> pending)
                => !(await pending.ConfigureAwait(false));
        }
        internal TResult ExecuteAggregate<TSource, TResult>(Query query, Aggregate aggregate)
        {
            IEnumerator<TSource>? iter = null;
            try
            {
                TSource value;
                switch (aggregate)
                {
                    // we *only* support operations that can be done by fetching one or two rows;
                    // we are *not* openly advocating for "push things automatically to LINQ-to-Objects"
                    case Aggregate.First when typeof(TSource) == typeof(TResult):
                        if (query.Take == 0) ThrowEmpty();
                        iter = GetEnumerator<TSource>(query.ApplyTake(1).RemoveDistinctIfNoSkip());
                        if (!iter.MoveNext()) ThrowEmpty();
                        value = iter.Current;
                        return Unsafe.As<TSource, TResult>(ref value);
                    case Aggregate.FirstOrDefault when typeof(TSource) == typeof(TResult):
                        if (query.Take == 0) return default!;
                        iter = GetEnumerator<TSource>(query.ApplyTake(1).RemoveDistinctIfNoSkip());
                        value = iter.MoveNext() ? iter.Current : default!;
                        return Unsafe.As<TSource, TResult>(ref value);
                    case Aggregate.Single when typeof(TSource) == typeof(TResult):
                        if (query.Take == 0) ThrowEmpty();
                        iter = GetEnumerator<TSource>(query.ApplyTake(2));
                        if (!iter.MoveNext()) ThrowEmpty();
                        value = iter.Current;
                        if (iter.MoveNext()) ThrowMultiple();
                        return Unsafe.As<TSource, TResult>(ref value);
                    case Aggregate.SingleOrDefault when typeof(TSource) == typeof(TResult):
                        if (query.Take == 0) return default!;
                        iter = GetEnumerator<TSource>(query.ApplyTake(2));
                        if (iter.MoveNext())
                        {
                            value = iter.Current;
                            if (iter.MoveNext()) ThrowMultiple();
                        }
                        else
                        {
                            value = default!;
                        }
                        return Unsafe.As<TSource, TResult>(ref value);
                    case Aggregate.Any when typeof(TResult) == typeof(bool):
                        if (query.Take == 0) return default!;
                        iter = GetEnumerator<TSource>(query.ApplyTake(1).RemoveDistinctIfNoSkip());
                        var any = iter.MoveNext();
                        return Unsafe.As<bool, TResult>(ref any);
                    case Aggregate.Count when typeof(TResult) == typeof(int) && AllowExpensiveAggregates:
                        if (query.Take == 0) return default!;
                        int i32 = 0;
                        iter = GetEnumerator<TSource>(query);
                        while (iter.MoveNext())
                            checked { i32++; }
                        return Unsafe.As<int, TResult>(ref i32);
                    case Aggregate.Count when typeof(TResult) == typeof(long) && AllowExpensiveAggregates:
                        if (query.Take == 0) return default!;
                        long i64 = 0;
                        iter = GetEnumerator<TSource>(query);
                        while (iter.MoveNext())
                            i64++;
                        return Unsafe.As<long, TResult>(ref i64);
                }
            }
            finally
            {
                iter?.Dispose();
            }
            throw new NotSupportedException($"The '{aggregate}' aggregate is not supported for this query by this provider; in some cases, you may be able to use .AsEnumerable() before your aggregate, but this may involve fetching large quantities of data");
        }

        /// <summary>
        /// Utility method for reading a single result from an iterator
        /// </summary>
        protected static TResult Single<TResult>(IEnumerator<TResult> iterator)
        {
            try
            {
                if (!iterator.MoveNext()) ThrowEmpty();
                var result = iterator.Current;
                if (iterator.MoveNext()) ThrowMultiple();
                return result;
            }
            finally
            {
                iterator?.Dispose();
            }
        }
        /// <summary>
        /// Utility method for reading a single result from an iterator
        /// </summary>
        protected static async ValueTask<TResult> SingleAsync<TResult>(IAsyncEnumerator<TResult> iterator)
        {
            try
            {
                if (!await iterator.MoveNextAsync().ConfigureAwait(false)) ThrowEmpty();
                var result = iterator.Current;
                if (await iterator.MoveNextAsync().ConfigureAwait(false)) ThrowMultiple();
                return result;
            }
            finally
            {
                if (iterator is not null)
                    await iterator.DisposeAsync().ConfigureAwait(false);
            }
        }

        internal async ValueTask<TResult> ExecuteAggregateAsync<TSource, TResult>(Query query, Aggregate aggregate, CancellationToken cancellationToken)
        {
            IAsyncEnumerator<TSource>? iter = null;
            try
            {
                TSource value;
                switch (aggregate)
                {
                    // we *only* support operations that can be done by fetching one or two rows;
                    // we are *not* openly advocating for "push things automatically to LINQ-to-Objects"
                    case Aggregate.First when typeof(TSource) == typeof(TResult):
                        if (query.Take == 0) ThrowEmpty();
                        iter = GetAsyncEnumerator<TSource>(query.ApplyTake(1).RemoveDistinctIfNoSkip(), cancellationToken);
                        if (!await iter.MoveNextAsync().ConfigureAwait(false)) ThrowEmpty();
                        value = iter.Current;
                        return Unsafe.As<TSource, TResult>(ref value);
                    case Aggregate.FirstOrDefault when typeof(TSource) == typeof(TResult):
                        if (query.Take == 0) return default!;
                        iter = GetAsyncEnumerator<TSource>(query.ApplyTake(1).RemoveDistinctIfNoSkip(), cancellationToken);
                        value = (await iter.MoveNextAsync().ConfigureAwait(false)) ? iter.Current : default!;
                        return Unsafe.As<TSource, TResult>(ref value);
                    case Aggregate.Single when typeof(TSource) == typeof(TResult):
                        if (query.Take == 0) ThrowEmpty();
                        iter = GetAsyncEnumerator<TSource>(query.ApplyTake(2), cancellationToken);
                        if (!await iter.MoveNextAsync().ConfigureAwait(false)) ThrowEmpty();
                        value = iter.Current;
                        if (await iter.MoveNextAsync().ConfigureAwait(false)) ThrowMultiple();
                        return Unsafe.As<TSource, TResult>(ref value);
                    case Aggregate.SingleOrDefault when typeof(TSource) == typeof(TResult):
                        if (query.Take == 0) return default!;
                        iter = GetAsyncEnumerator<TSource>(query.ApplyTake(2), cancellationToken);
                        if (await iter.MoveNextAsync().ConfigureAwait(false))
                        {
                            value = iter.Current;
                            if (await iter.MoveNextAsync().ConfigureAwait(false)) ThrowMultiple();
                        }
                        else
                        {
                            value = default!;
                        }
                        return Unsafe.As<TSource, TResult>(ref value);
                    case Aggregate.Any when typeof(TResult) == typeof(bool):
                        if (query.Take == 0) return default!;
                        iter = GetAsyncEnumerator<TSource>(query.ApplyTake(1).RemoveDistinctIfNoSkip(), cancellationToken);
                        var any = await iter.MoveNextAsync().ConfigureAwait(false);
                        return Unsafe.As<bool, TResult>(ref any);
                    case Aggregate.Count when typeof(TResult) == typeof(int) && AllowExpensiveAggregates:
                        if (query.Take == 0) return default!;
                        int i32 = 0;
                        iter = GetAsyncEnumerator<TSource>(query, cancellationToken);
                        while (await iter.MoveNextAsync().ConfigureAwait(false))
                            checked { i32++; }
                        return Unsafe.As<int, TResult>(ref i32);
                    case Aggregate.Count when typeof(TResult) == typeof(long) && AllowExpensiveAggregates:
                        if (query.Take == 0) return default!;
                        long i64 = 0;
                        iter = GetAsyncEnumerator<TSource>(query, cancellationToken);
                        while (await iter.MoveNextAsync().ConfigureAwait(false))
                            i64++;
                        return Unsafe.As<long, TResult>(ref i64);
                }

                throw new NotSupportedException($"The '{aggregate}' aggregate is not supported for this query by this provider; in some cases, you may be able to use .AsEnumerable() before your aggregate, but this may involve fetching large quantities of data");
            }
            finally
            {
                if (iter is not null)
                    await iter.DisposeAsync().ConfigureAwait(false);
            }
        }

        static readonly object[] s_multiple = new object[2];
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowEmpty() => Array.Empty<object>().First();

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowMultiple() => s_multiple.Single();

        /// <summary>
        /// Get a synchronous iterator for the given <see cref="Query"/>
        /// </summary>
        protected internal abstract IEnumerator<TElement> GetEnumerator<TElement>(Query query);
        /// <summary>
        /// Get an asynchronous iterator for the given <see cref="Query"/>
        /// </summary>
        protected internal abstract IAsyncEnumerator<TElement> GetAsyncEnumerator<TElement>(Query query, CancellationToken cancellationToken);

        /// <summary>
        /// After asserting that the types are the same, safely perform an in-place type coercion; this is useful for implementing <see cref="ExecuteAggregate{TResult}(Query, Aggregate)"/>
        /// </summary>
        protected internal static TTo Coerce<TFrom, TTo>(TFrom value)
        {
            if (typeof(TFrom) == typeof(TTo))
                return Unsafe.As<TFrom, TTo>(ref value);
            ThrowCoercionFailure(typeof(TFrom), typeof(TTo));
            return default!;
        }
        /// <summary>
        /// After asserting that the types are the same, safely perform an in-place type coercion; this is useful for implementing <see cref="ExecuteAggregateAsync{TResult}(Query, Aggregate, CancellationToken)"/>
        /// </summary>
        protected internal static ValueTask<TTo> CoerceAsync<TFrom, TTo>(TFrom value)
        {
            if (typeof(TFrom) == typeof(TTo))
                return new(Unsafe.As<TFrom, TTo>(ref value));
            ThrowCoercionFailure(typeof(TFrom), typeof(TTo));
            return default!;
        }

        [MethodImpl(MethodImplOptions.NoInlining), DoesNotReturn]
        private static void ThrowCoercionFailure(Type from, Type to) => throw new InvalidCastException($"Invalid coercion from '{from?.Name}' to '{to?.Name}'");

        internal static bool TryResolveAggregate(Expression expression, [NotNullWhen(true)] out Query? query, out Aggregate aggregate)
        {
            if (IsFromQueryable(expression, out var method, out var args, out query, out var argCount))
            {
                LambdaExpression? lambda = null;
                if (argCount > 0)
                {
                    if (TryGetLambda(args[1], out lambda))
                        argCount--; // accounted for; simplifies checks below
                }

                const string Async = "Async";

                switch (method.Name)
                {
                    case nameof(Queryable.Count) when argCount == 0:
                    case nameof(Queryable.LongCount) when argCount == 0:
                    case nameof(Queryable.Count) + Async when argCount == 0:
                    case nameof(Queryable.LongCount) + Async when argCount == 0:
                        if (lambda is not null) query = query.ApplyWhere(lambda);
                        aggregate = Aggregate.Count;
                        return true;
                    case nameof(Queryable.Single) when argCount == 0:
                    case nameof(Queryable.Single) + Async when argCount == 0:
                        if (lambda is not null) query = query.ApplyWhere(lambda);
                        aggregate = Aggregate.Single;
                        return true;
                    case nameof(Queryable.SingleOrDefault) when argCount == 0:
                    case nameof(Queryable.SingleOrDefault) + Async when argCount == 0:
                        if (lambda is not null) query = query.ApplyWhere(lambda);
                        aggregate = Aggregate.SingleOrDefault;
                        return true;
                    case nameof(Queryable.First) when argCount == 0:
                    case nameof(Queryable.First) + Async when argCount == 0:
                        if (lambda is not null) query = query.ApplyWhere(lambda);
                        aggregate = Aggregate.First;
                        return true;
                    case nameof(Queryable.FirstOrDefault) when argCount == 0:
                    case nameof(Queryable.FirstOrDefault) + Async when argCount == 0:
                        if (lambda is not null) query = query.ApplyWhere(lambda);
                        aggregate = Aggregate.FirstOrDefault;
                        return true;
                    case nameof(Queryable.Last) when argCount == 0:
                    case nameof(Queryable.Last) + Async when argCount == 0:
                        if (lambda is not null) query = query.ApplyWhere(lambda);
                        query = query.ApplyReverse();
                        aggregate = Aggregate.First;
                        return true;
                    case nameof(Queryable.LastOrDefault) when argCount == 0:
                    case nameof(Queryable.LastOrDefault) + Async when argCount == 0:
                        if (lambda is not null) query = query.ApplyWhere(lambda);
                        query = query.ApplyReverse();
                        aggregate = Aggregate.FirstOrDefault;
                        return true;
                    case nameof(Queryable.Any) when argCount == 0:
                    case nameof(Queryable.Any) + Async when argCount == 0:
                        if (lambda is not null) query = query.ApplyWhere(lambda);
                        aggregate = Aggregate.Any;
                        return true;
                    case nameof(Queryable.Average) when argCount == 0:
                    case nameof(Queryable.Average) + Async when argCount == 0:
                        if (lambda is not null) query = query.ApplySelect(lambda);
                        aggregate = Aggregate.Average;
                        return true;
                    case nameof(Queryable.Sum) when argCount == 0:
                    case nameof(Queryable.Sum) + Async when argCount == 0:
                        if (lambda is not null) query = query.ApplySelect(lambda);
                        aggregate = Aggregate.Sum;
                        return true;
                    case nameof(Queryable.Min) when argCount == 0:
                    case nameof(Queryable.Min) + Async when argCount == 0:
                        if (lambda is not null) query = query.ApplySelect(lambda);
                        aggregate = Aggregate.Minimum;
                        return true;
                    case nameof(Queryable.Max) when argCount == 0:
                    case nameof(Queryable.Max) + Async when argCount == 0:
                        if (lambda is not null) query = query.ApplySelect(lambda);
                        aggregate = Aggregate.Maximum;
                        return true;
                    case nameof(Queryable.All) when argCount == 0:
                    case nameof(Queryable.All) + Async when argCount == 0:
                        if (lambda is not null)
                        {
                            query = query.ApplyWhere(lambda.Negate());
                            aggregate = Aggregate.NotAny;
                            return true;
                        }
                        break;
                    case nameof(Queryable.ElementAt) when argCount == 1 && lambda is null:
                    case nameof(Queryable.ElementAt) + Async when argCount == 1 && lambda is null:
                        if (TryApplyIndex(ref query, args))
                        {
                            aggregate = Aggregate.First;
                            return true;
                        }
                        break;
                    case nameof(Queryable.ElementAtOrDefault) when argCount == 1 && lambda is null:
                    case nameof(Queryable.ElementAtOrDefault) + Async when argCount == 1 && lambda is null:
                        if (TryApplyIndex(ref query, args))
                        {
                            aggregate = Aggregate.FirstOrDefault;
                            return true;
                        }
                        break;
                    case nameof(Queryable.Contains) when argCount == 1 && lambda is null:
                    case nameof(Queryable.Contains) + Async when argCount == 1 && lambda is null:
                        var p = Expression.Parameter(query.ElementType, "source");
                        var where = Expression.Lambda(Expression.Equal(p, args[1]), p);
                        query = query.ApplyWhere(where);
                        aggregate = Aggregate.Any;
                        return true;
                }
            }
            aggregate = default;
            return false;

            static bool TryApplyIndex(ref Query query, ReadOnlyCollection<Expression> args)
            {
                if (args.Count > 1 && args[1].TryGetConstantValue(out var value, out _))
                {
                    if (value is int i)
                    {
                        query = query.ApplySkip(i);
                        return true;
                    }
                    if (value is long l)
                    {
                        query = query.ApplySkip(l);
                        return true;
                    }
                    if (value is Index ix)
                    {
                        query = query.ApplySkipForElementAt(ix);
                        return true;
                    }
                }
                return false;
            }
        }

        internal TResult Execute<TResult>(Expression expression)
        {
            if (TryResolveAggregate(expression, out var query, out var aggregate))
            {
                return ExecuteAggregate<TResult>(query, aggregate);
            }
            ThrowNotSupported(expression);
            return default!;
        }
    }
}
