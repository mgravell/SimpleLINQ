using System;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleLINQ.Transports
{
    partial class Transport
    {
        /// <summary>
        /// A <see cref="Transport"/> that cannot talk to any data source - useful for unit testing only.
        /// </summary>
        public static Transport Null => NullTransport.Instance;

        private sealed class NullTransport : Transport
        {
            private static NullTransport? s_instance;
            public static NullTransport Instance => s_instance ??= new NullTransport();
            private NullTransport() { }
            public override object? Execute(ReadOnlySpan<object?> command)
                => throw new NotSupportedException("Execution is not possible on the null transport");

            public override ValueTask<object?> ExecuteAsync(ReadOnlyMemory<object?> command, CancellationToken cancellationToken)
                => new(Execute(command.Span));
        }
    }
}
