using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleLINQ.Async
{
    partial class QueryableAsyncExtensions
    {
        private static ValueTask<TSource> AggregateAsync<TSource>(IQueryable<TSource> source, Aggregate aggregate, CancellationToken cancellationToken)
            => AggregateAsync<TSource, TSource>(source, aggregate, cancellationToken);
        private static ValueTask<TResult> AggregateAsync<TSource, TResult>(IQueryable<TSource> source, Aggregate aggregate, CancellationToken cancellationToken)
        {
            var query = GetQuery(source);
            return query.Provider.ExecuteAggregateAsync<TResult>(query, aggregate, cancellationToken);
        }

        private static ValueTask<TResult> AggregateAsync<TSource, TResult>(IQueryable<TSource> source, Expression<Func<TSource, TResult>> selector, Aggregate aggregate, CancellationToken cancellationToken)
            => AggregateAsync<TSource, TResult, TResult>(source, selector, aggregate, cancellationToken);
        private static ValueTask<TResult> AggregateAsync<TSource, TAccumulate, TResult>(IQueryable<TSource> source, Expression<Func<TSource, TAccumulate>> selector, Aggregate aggregate, CancellationToken cancellationToken)
        {
            var query = GetQuery(source).ApplySelect(selector);
            return query.Provider.ExecuteAggregateAsync<TResult>(query, aggregate, cancellationToken);
        }


        /// <summary>
        /// Asynchronous version of <see cref="Queryable.Average(IQueryable{double?})"/>.
        /// </summary>
        public static ValueTask<double?> AverageAsync(this IQueryable<double?> source, CancellationToken cancellationToken = default)
            => AggregateAsync(source, Aggregate.Average, cancellationToken);
        /// <summary>
        /// Asynchronous version of <see cref="Queryable.Average(IQueryable{decimal})"/>.
        /// </summary>
        public static ValueTask<decimal> AverageAsync(this IQueryable<decimal> source, CancellationToken cancellationToken = default)
            => AggregateAsync(source, Aggregate.Average, cancellationToken);
        /// <summary>
        /// Asynchronous version of <see cref="Queryable.Average(IQueryable{decimal?})"/>.
        /// </summary>
        public static ValueTask<decimal?> AverageAsync(this IQueryable<decimal?> source, CancellationToken cancellationToken = default)
            => AggregateAsync(source, Aggregate.Average, cancellationToken);
        /// <summary>
        /// Asynchronous version of <see cref="Queryable.Average(IQueryable{int})"/>.
        /// </summary>
        public static ValueTask<double> AverageAsync(this IQueryable<int> source, CancellationToken cancellationToken = default)
            => AggregateAsync<int, double>(source, Aggregate.Average, cancellationToken);
        /// <summary>
        /// Asynchronous version of <see cref="Queryable.Average(IQueryable{int?})"/>.
        /// </summary>
        public static ValueTask<double?> AverageAsync(this IQueryable<int?> source, CancellationToken cancellationToken = default)
            => AggregateAsync<int?, double?>(source, Aggregate.Average, cancellationToken);
        /// <summary>
        /// Asynchronous version of <see cref="Queryable.Average(IQueryable{long})"/>.
        /// </summary>
        public static ValueTask<double> AverageAsync(this IQueryable<long> source, CancellationToken cancellationToken = default)
            => AggregateAsync<long, double>(source, Aggregate.Average, cancellationToken);
        /// <summary>
        /// Asynchronous version of <see cref="Queryable.Average(IQueryable{long?})"/>.
        /// </summary>
        public static ValueTask<double?> AverageAsync(this IQueryable<long?> source, CancellationToken cancellationToken = default)
            => AggregateAsync<long?, double?>(source, Aggregate.Average, cancellationToken);
        /// <summary>
        /// Asynchronous version of <see cref="Queryable.Average(IQueryable{float})"/>.
        /// </summary>
        public static ValueTask<float> AverageAsync(this IQueryable<float> source, CancellationToken cancellationToken = default)
            => AggregateAsync(source, Aggregate.Average, cancellationToken);
        /// <summary>
        /// Asynchronous version of <see cref="Queryable.Average(IQueryable{float?})"/>.
        /// </summary>
        public static ValueTask<float?> AverageAsync(this IQueryable<float?> source, CancellationToken cancellationToken = default)
            => AggregateAsync(source, Aggregate.Average, cancellationToken);
        /// <summary>
        /// Asynchronous version of <see cref="Queryable.Average(IQueryable{double})"/>.
        /// </summary>
        public static ValueTask<double> AverageAsync(this IQueryable<double> source, CancellationToken cancellationToken = default)
            => AggregateAsync(source, Aggregate.Average, cancellationToken);
        /// <summary>
        /// Asynchronous version of <see cref="Queryable.Average{TSource}(IQueryable{TSource}, Expression{Func{TSource, int}})"/>.
        /// </summary>
        public static ValueTask<double> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int>> selector, CancellationToken cancellationToken = default)
            => AggregateAsync<TSource, int, double>(source, selector, Aggregate.Average, cancellationToken);
        /// <summary>
        /// Asynchronous version of <see cref="Queryable.Average{TSource}(IQueryable{TSource}, Expression{Func{TSource, int?}})"/>.
        /// </summary>
        public static ValueTask<double?> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int?>> selector, CancellationToken cancellationToken = default)
            => AggregateAsync<TSource, int?, double?>(source, selector, Aggregate.Average, cancellationToken);
        /// <summary>
        /// Asynchronous version of <see cref="Queryable.Average{TSource}(IQueryable{TSource}, Expression{Func{TSource, float}})"/>.
        /// </summary>
        public static ValueTask<float> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, float>> selector, CancellationToken cancellationToken = default)
            => AggregateAsync(source, selector, Aggregate.Average, cancellationToken);
        /// <summary>
        /// Asynchronous version of <see cref="Queryable.Average{TSource}(IQueryable{TSource}, Expression{Func{TSource, float?}})"/>.
        /// </summary>
        public static ValueTask<float?> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, float?>> selector, CancellationToken cancellationToken = default)
            => AggregateAsync(source, selector, Aggregate.Average, cancellationToken);
        /// <summary>
        /// Asynchronous version of <see cref="Queryable.Average{TSource}(IQueryable{TSource}, Expression{Func{TSource, long}})"/>.
        /// </summary>
        public static ValueTask<double> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, long>> selector, CancellationToken cancellationToken = default)
            => AggregateAsync<TSource, long, double>(source, selector, Aggregate.Average, cancellationToken);
        /// <summary>
        /// Asynchronous version of <see cref="Queryable.Average{TSource}(IQueryable{TSource}, Expression{Func{TSource, long?}})"/>.
        /// </summary>
        public static ValueTask<double?> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, long?>> selector, CancellationToken cancellationToken = default)
            => AggregateAsync<TSource, long?, double?>(source, selector, Aggregate.Average, cancellationToken);
        /// <summary>
        /// Asynchronous version of <see cref="Queryable.Average{TSource}(IQueryable{TSource}, Expression{Func{TSource, double}})"/>.
        /// </summary>
        public static ValueTask<double> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, double>> selector, CancellationToken cancellationToken = default)
            => AggregateAsync(source, selector, Aggregate.Average, cancellationToken);
        /// <summary>
        /// Asynchronous version of <see cref="Queryable.Average{TSource}(IQueryable{TSource}, Expression{Func{TSource, double?}})"/>.
        /// </summary>
        public static ValueTask<double?> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, double?>> selector, CancellationToken cancellationToken = default)
            => AggregateAsync(source, selector, Aggregate.Average, cancellationToken);
        /// <summary>
        /// Asynchronous version of <see cref="Queryable.Average{TSource}(IQueryable{TSource}, Expression{Func{TSource, decimal}})"/>.
        /// </summary>
        public static ValueTask<decimal> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, decimal>> selector, CancellationToken cancellationToken = default)
            => AggregateAsync(source, selector, Aggregate.Average, cancellationToken);
        /// <summary>
        /// Asynchronous version of <see cref="Queryable.Average{TSource}(IQueryable{TSource}, Expression{Func{TSource, decimal?}})"/>.
        /// </summary>
        public static ValueTask<decimal?> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, decimal?>> selector, CancellationToken cancellationToken = default)
            => AggregateAsync(source, selector, Aggregate.Average, cancellationToken);
        /// <summary>
        /// Asynchronous version of <see cref="Queryable.Sum(IQueryable{int})"/>.
        /// </summary>
        public static ValueTask<int> SumAsync(this IQueryable<int> source, CancellationToken cancellationToken = default)
            => AggregateAsync(source, Aggregate.Sum, cancellationToken);
        /// <summary>
        /// Asynchronous version of <see cref="Queryable.Sum(IQueryable{int?})"/>.
        /// </summary>
        public static ValueTask<int?> SumAsync(this IQueryable<int?> source, CancellationToken cancellationToken = default)
            => AggregateAsync(source, Aggregate.Sum, cancellationToken);
        /// <summary>
        /// Asynchronous version of <see cref="Queryable.Sum(IQueryable{long})"/>.
        /// </summary>
        public static ValueTask<long> SumAsync(this IQueryable<long> source, CancellationToken cancellationToken = default)
            => AggregateAsync(source, Aggregate.Sum, cancellationToken);
        /// <summary>
        /// Asynchronous version of <see cref="Queryable.Sum(IQueryable{long?})"/>.
        /// </summary>
        public static ValueTask<long?> SumAsync(this IQueryable<long?> source, CancellationToken cancellationToken = default)
            => AggregateAsync(source, Aggregate.Sum, cancellationToken);
        /// <summary>
        /// Asynchronous version of <see cref="Queryable.Sum(IQueryable{float})"/>.
        /// </summary>
        public static ValueTask<float> SumAsync(this IQueryable<float> source, CancellationToken cancellationToken = default)
            => AggregateAsync(source, Aggregate.Sum, cancellationToken);
        /// <summary>
        /// Asynchronous version of <see cref="Queryable.Sum(IQueryable{float?})"/>.
        /// </summary>
        public static ValueTask<float?> SumAsync(this IQueryable<float?> source, CancellationToken cancellationToken = default)
            => AggregateAsync(source, Aggregate.Sum, cancellationToken);
        /// <summary>
        /// Asynchronous version of <see cref="Queryable.Sum(IQueryable{double})"/>.
        /// </summary>
        public static ValueTask<double> SumAsync(this IQueryable<double> source, CancellationToken cancellationToken = default)
            => AggregateAsync(source, Aggregate.Sum, cancellationToken);
        /// <summary>
        /// Asynchronous version of <see cref="Queryable.Sum(IQueryable{double?})"/>.
        /// </summary>
        public static ValueTask<double?> SumAsync(this IQueryable<double?> source, CancellationToken cancellationToken = default)
            => AggregateAsync(source, Aggregate.Sum, cancellationToken);
        /// <summary>
        /// Asynchronous version of <see cref="Queryable.Sum(IQueryable{decimal})"/>.
        /// </summary>
        public static ValueTask<decimal> SumAsync(this IQueryable<decimal> source, CancellationToken cancellationToken = default)
            => AggregateAsync(source, Aggregate.Sum, cancellationToken);
        /// <summary>
        /// Asynchronous version of <see cref="Queryable.Sum(IQueryable{decimal?})"/>.
        /// </summary>
        public static ValueTask<decimal?> SumAsync(this IQueryable<decimal?> source, CancellationToken cancellationToken = default)
            => AggregateAsync(source, Aggregate.Sum, cancellationToken);
        /// <summary>
        /// Asynchronous version of <see cref="Queryable.Sum{TSource}(IQueryable{TSource}, Expression{Func{TSource, int}})"/>.
        /// </summary>
        public static ValueTask<int> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int>> selector, CancellationToken cancellationToken = default)
            => AggregateAsync(source, selector, Aggregate.Sum, cancellationToken);
        /// <summary>
        /// Asynchronous version of <see cref="Queryable.Sum{TSource}(IQueryable{TSource}, Expression{Func{TSource, int?}})"/>.
        /// </summary>
        public static ValueTask<int?> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int?>> selector, CancellationToken cancellationToken = default)
            => AggregateAsync(source, selector, Aggregate.Sum, cancellationToken);
        /// <summary>
        /// Asynchronous version of <see cref="Queryable.Sum{TSource}(IQueryable{TSource}, Expression{Func{TSource, long}})"/>.
        /// </summary>
        public static ValueTask<long> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, long>> selector, CancellationToken cancellationToken = default)
            => AggregateAsync(source, selector, Aggregate.Sum, cancellationToken);
        /// <summary>
        /// Asynchronous version of <see cref="Queryable.Sum{TSource}(IQueryable{TSource}, Expression{Func{TSource, long?}})"/>.
        /// </summary>
        public static ValueTask<long?> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, long?>> selector, CancellationToken cancellationToken = default)
            => AggregateAsync(source, selector, Aggregate.Sum, cancellationToken);
        /// <summary>
        /// Asynchronous version of <see cref="Queryable.Sum{TSource}(IQueryable{TSource}, Expression{Func{TSource, float}})"/>.
        /// </summary>
        public static ValueTask<float> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, float>> selector, CancellationToken cancellationToken = default)
            => AggregateAsync(source, selector, Aggregate.Sum, cancellationToken);
        /// <summary>
        /// Asynchronous version of <see cref="Queryable.Sum{TSource}(IQueryable{TSource}, Expression{Func{TSource, float?}})"/>.
        /// </summary>
        public static ValueTask<float?> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, float?>> selector, CancellationToken cancellationToken = default)
            => AggregateAsync(source, selector, Aggregate.Sum, cancellationToken);
        /// <summary>
        /// Asynchronous version of <see cref="Queryable.Sum{TSource}(IQueryable{TSource}, Expression{Func{TSource, double}})"/>.
        /// </summary>
        public static ValueTask<double> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, double>> selector, CancellationToken cancellationToken = default)
            => AggregateAsync(source, selector, Aggregate.Sum, cancellationToken);
        /// <summary>
        /// Asynchronous version of <see cref="Queryable.Sum{TSource}(IQueryable{TSource}, Expression{Func{TSource, double?}})"/>.
        /// </summary>
        public static ValueTask<double?> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, double?>> selector, CancellationToken cancellationToken = default)
            => AggregateAsync(source, selector, Aggregate.Sum, cancellationToken);
        /// <summary>
        /// Asynchronous version of <see cref="Queryable.Sum{TSource}(IQueryable{TSource}, Expression{Func{TSource, decimal}})"/>.
        /// </summary>
        public static ValueTask<decimal> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, decimal>> selector, CancellationToken cancellationToken = default)
            => AggregateAsync(source, selector, Aggregate.Sum, cancellationToken);
        /// <summary>
        /// Asynchronous version of <see cref="Queryable.Sum{TSource}(IQueryable{TSource}, Expression{Func{TSource, decimal?}})"/>.
        /// </summary>
        public static ValueTask<decimal?> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, decimal?>> selector, CancellationToken cancellationToken = default)
            => AggregateAsync(source, selector, Aggregate.Sum, cancellationToken);
    }
}
