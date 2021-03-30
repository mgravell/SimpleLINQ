using System.Linq;
using System.Reflection;

namespace SimpleLINQ.Async.Internal
{
    internal static class AsyncTypeHelper
    {
        internal static readonly MethodInfo CreateAsyncQueryTemplate = typeof(AsyncTypeHelper).GetMethods(
            BindingFlags.Static | BindingFlags.NonPublic).Single(x => x.Name == nameof(CreateAsyncQuery));

        private static Query CreateAsyncQuery<T>(Query tail) => new AsyncQuery<T>(tail);
    }
}
