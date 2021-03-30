using ServiceStack.Redis;
using SimpleLINQ.Transports;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleLINQ.ServiceStack.Redis
{
    /// <summary>
    /// A <see cref="Transport"/> implementation that can talk to Redis via ServiceStack.Redis
    /// </summary>
    public class NativeClientTransport : Transport
    {
        private readonly IRedisNativeClient _client;
        /// <summary>
        /// Create a new instance
        /// </summary>
        public NativeClientTransport(IRedisNativeClient client)
            => _client = client ?? throw new ArgumentNullException(nameof(client));

        /// <inheritdoc/>
        public override object? Execute(ReadOnlySpan<object?> command)
        {
            if (command.IsEmpty)
                throw new InvalidOperationException("No command to issue");
            return _client.RawCommand(command.ToArray());
        }

        /// <inheritdoc/>
        public override async ValueTask<object?> ExecuteAsync(ReadOnlyMemory<object?> command, CancellationToken cancellationToken)
        {
            if (command.IsEmpty)
                throw new InvalidOperationException("No command to issue");
            if (_client is not IRedisNativeClientAsync asyncClient)
                throw new InvalidOperationException($"The redis client ('{_client.GetType().FullName}') does not support async operations");
            return await asyncClient.RawCommandAsync(command.Span.ToArray(), cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public override object? GetArrayItem(object value, int index)
        {
            if (value is RedisData rd && rd.Children is { } typed)
            {
                return typed[index];
            }
            return base.GetArrayItem(value, index);
        }

        /// <inheritdoc/>
        public override void CopyArrayTo(object value, Span<object?> target)
        {
            if (value is RedisData rd && rd.Children is { } typed)
            {
                int index = 0;
                foreach (var child in typed)
                {
                    target[index++] = child;
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
            if (value is RedisData rd)
            {
                if (rd.Children is { } typed)
                {
                    length = typed.Count;
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
            if (value is RedisData rd)
            {
                return checked((int)rd.ToInt64());
            }
            return base.GetInt32(value);
        }

        /// <inheritdoc/>
        public override long GetInt64(object? value)
        {
            if (value is RedisData rd)
            {
                return rd.ToInt64();
            }
            return base.GetInt64(value);
        }

        /// <inheritdoc/>
        public override string? GetString(object? value)
        {
            if (value is RedisData rd)
            {
                return rd.Data is null ? null : Encoding.UTF8.GetString(rd.Data);
            }
            return base.GetString(value);
        }

        /// <inheritdoc/>
        public override double GetDouble(object? value)
        {
            if (value is RedisData rd)
            {
                return rd.ToDouble();
            }
            return base.GetDouble(value);
        }

        /// <inheritdoc/>
        public override float GetSingle(object? value)
        {
            if (value is RedisData rd)
            {
                return (float)rd.ToDouble();
            }
            return base.GetSingle(value);
        }
    }
}
