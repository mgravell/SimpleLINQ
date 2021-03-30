using SimpleLINQ.Transports;
using StackExchange.Redis;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleLINQ.StackExchange.Redis
{
    /// <summary>
    /// A <see cref="Transport"/> implementation that can talk to Redis via StackExchange.Redis
    /// </summary>
    public class DatabaseTransport : Transport
    {
        private readonly IDatabase _db;
        /// <summary>
        /// Create a new instance
        /// </summary>
        public DatabaseTransport(IDatabase db)
            => _db = db ?? throw new ArgumentNullException(nameof(db));

        private static string GetCommand(ReadOnlySpan<object?> command, out object?[] args)
        {
            string? cmd = command.IsEmpty ? null : command[0]?.ToString();
            if (string.IsNullOrWhiteSpace(cmd))
                throw new InvalidOperationException("No command to issue");
            args = command.Slice(1).ToArray();
            return cmd;
        }
        /// <inheritdoc/>
        public override object? Execute(ReadOnlySpan<object?> command)
        {
            var cmd = GetCommand(command, out var args);
            return _db.Execute(cmd, args);
        }
        /// <inheritdoc/>
        public override async ValueTask<object?> ExecuteAsync(ReadOnlyMemory<object?> command, CancellationToken cancellationToken)
        {
            var cmd = GetCommand(command.Span, out var args);
            return await _db.ExecuteAsync(cmd, args).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public override object? GetArrayItem(object value, int index)
        {
            if (value is RedisResult rr && rr.Type == ResultType.MultiBulk)
            {
                var typed = (RedisResult[])rr;
                return typed[index];
            }
            return base.GetArrayItem(value, index);
        }
        /// <inheritdoc/>
        public override void CopyArrayTo(object value, Span<object?> target)
        {
            if (value is RedisResult rr && rr.Type == ResultType.MultiBulk)
            {
                var typed = (RedisResult[])rr;
                for (int i = 0; i < typed.Length; i++)
                {
                    target[i] = typed[i];
                }
            }
            else
            {
                base.CopyArrayTo(value, target);
            }
        }
        /// <inheritdoc/>
        public override bool IsArray([NotNullWhen(true)] object? value, out int length)
        {
            if (value is RedisResult rr)
            {
                if (rr.Type == ResultType.MultiBulk)
                {
                    if (rr.IsNull)
                    {
                        length = -1;
                    }
                    else
                    {
                        var typed = (RedisResult[])rr;
                        length = typed is null ? -1 : typed.Length;
                    }
                    return true;
                }
                else
                {
                    length = default;
                    return false;
                }
            }
            return base.IsArray(value, out length);
        }

        /// <inheritdoc/>
        public override int GetInt32(object? value)
        {
            if (value is RedisResult rr)
            {
                return (int)rr;
            }
            return base.GetInt32(value);
        }
        /// <inheritdoc/>
        public override long GetInt64(object? value)
        {
            if (value is RedisResult rr)
            {
                return (long)rr;
            }
            return base.GetInt64(value);
        }

        /// <inheritdoc/>
        public override string? GetString(object? value)
        {
            if (value is RedisResult rr)
            {
                return (string)rr;
            }
            return base.GetString(value);
        }
        /// <inheritdoc/>
        public override double GetDouble(object? value)
        {
            if (value is RedisResult rr)
            {
                return (double)rr;
            }
            return base.GetDouble(value);
        }
        /// <inheritdoc/>
        public override float GetSingle(object? value)
        {
            if (value is RedisResult rr)
            {
                return (float)(double)rr;
            }
            return base.GetSingle(value);
        }
    }
}
