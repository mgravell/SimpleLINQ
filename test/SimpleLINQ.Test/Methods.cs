using SimpleLINQ.Async;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Xunit;
using Xunit.Abstractions;

namespace SimpleLINQ.Test
{
    public class Methods
    {
        private readonly ITestOutputHelper _log;
        public Methods(ITestOutputHelper log)
            => _log = log;
        private void Log(string message)
            => _log?.WriteLine(message);

        [Theory]
        // [InlineData(typeof(AsyncQueryable), false)]
        [InlineData(typeof(Queryable), true)]
        public void AllMethodsCovered(Type source, bool makeAsync)
        {
            var theirs = source.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(x => x.Name switch
                { // we're *really* just after the aggregates here!

                    // intentially not supported
                    nameof(Queryable.Join) => false,
                    nameof(Queryable.GroupBy) => false,
                    nameof(Queryable.GroupJoin) => false,
                    nameof(Queryable.Zip) => false,
                    nameof(Queryable.OfType) => false,
                    nameof(Queryable.Cast) => false,
                    nameof(Queryable.Concat) => false,
                    nameof(Queryable.Append) => false,
                    nameof(Queryable.Prepend) => false,
                    nameof(Queryable.Intersect) => false,
                    nameof(Queryable.Union) => false,
                    nameof(Queryable.SkipLast) => false,
                    nameof(Queryable.SkipWhile) => false,
                    nameof(Queryable.TakeLast) => false,
                    nameof(Queryable.TakeWhile) => false,
                    nameof(Queryable.SequenceEqual) => false,
                    nameof(Queryable.SelectMany) => false,
                    nameof(Queryable.Except) => false,
                    nameof(Queryable.AsQueryable) => false,
                    nameof(Queryable.DefaultIfEmpty) => false,
                    nameof(Queryable.Aggregate) => false, // way to vague - need specific aggregates
                    // don't support custom equality on Contains, but regular equality is fine
                    nameof(Queryable.Contains) when x.GetParameters().Any(
                        p => p.ParameterType.IsGenericType
                        && p.ParameterType.GetGenericTypeDefinition() == typeof(IEqualityComparer<>)) => false,

                    // supported via core API
                    nameof(Queryable.OrderBy) => false,
                    nameof(Queryable.OrderByDescending) => false,
                    nameof(Queryable.ThenBy) => false,
                    nameof(Queryable.ThenByDescending) => false,
                    nameof(Queryable.Select) => false,
                    nameof(Queryable.Where) => false,
                    nameof(Queryable.Skip) => false,
                    nameof(Queryable.Take) => false,
                    nameof(Queryable.Distinct) => false,
                    nameof(Queryable.Reverse) => false,

                    // include everything else
                    _ => true,
                })
                .Select(x => NormalizeMethodSignature(x, makeAsync)).ToHashSet();
            
            var mine = typeof(QueryableAsyncExtensions).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(x => x.Name switch
                {
                    // known additional APIs
                    nameof(QueryableAsyncExtensions.ToListAsync) => false,
                    nameof(QueryableAsyncExtensions.ToArrayAsync) => false,
                    nameof(QueryableAsyncExtensions.AsAsyncEnumerable) => false,

                    // include everything else
                    _ => true,
                })
                .Select(x => NormalizeMethodSignature(x, false)).ToHashSet();

            Log($"{mine.Count} vs {theirs.Count}");
            int failures = 0;
            Log("====== Missing APIs:");
            foreach (var method in theirs.Where(x => !mine.Contains(x)).OrderBy(x => x))
            {
                failures++;
                Log(method);
            }
            Log("====== Extra APIs:");
            foreach (var method in mine.Where(x => !theirs.Contains(x)).OrderBy(x => x))
            {
                failures++;
                Log(method);
            }
            Assert.Equal(0, failures);
        }

        static string NormalizeMethodSignature(MethodInfo method, bool makeAsync)
        {
            var sb = new StringBuilder();
            if (makeAsync) sb.Append("ValueTask<");
            AppendTypeName(method.ReturnType, sb);
            if (makeAsync) sb.Append(">");

            sb.Append(' ').Append(makeAsync ? method.Name + "Async" : method.Name);
            if (method.IsGenericMethodDefinition)
            {
                var targs = method.GetGenericArguments();
                sb.Append('<').Append(targs[0].Name);
                for (int i = 1; i < targs.Length; i++)
                {
                    sb.Append(", ").Append(targs[i].Name);
                }
                sb.Append('>');
            }
            sb.Append('(');
            var pargs = method.GetParameters();
            for (int i = 0; i < pargs.Length; i++)
            {
                if (i == 0)
                {
                    if (method.IsDefined(typeof(ExtensionAttribute)))
                    {
                        sb.Append("this ");
                    }
                }
                else
                {
                    sb.Append(", ");
                }
                var p = pargs[i];
                AppendTypeName(p.ParameterType, sb).Append(' ').Append(p.Name);
                if (p.IsOptional)
                {
                    sb.Append(" = ");
                    if (p.HasDefaultValue)
                    {
                        sb.Append(p.DefaultValue switch
                        {
                            null => "default",
                            string s => "\"" + s.Replace("\"", "\"\"") + "\"",
                            bool b => b ? "true" : "false",
                            object o => Convert.ToString(o, CultureInfo.InvariantCulture),
                        }); ;
                    }
                    else
                    {
                        sb.Append("default");
                    }
                }
            }
            if (makeAsync)
            {
                if (pargs.Length != 0) sb.Append(", ");
                AppendTypeName(typeof(CancellationToken), sb).Append(" cancellationToken = default");
            }

            return sb.Append(')').ToString();

            static StringBuilder AppendTypeName(Type type, StringBuilder sb)
            {
                var nt = Nullable.GetUnderlyingType(type);
                if (nt is not null)
                {
                    return AppendTypeName(nt, sb).Append('?');
                }
                if (type == typeof(int)) return sb.Append("int");
                if (type == typeof(uint)) return sb.Append("uint");
                if (type == typeof(long)) return sb.Append("long");
                if (type == typeof(ulong)) return sb.Append("ulong");
                if (type == typeof(short)) return sb.Append("short");
                if (type == typeof(ushort)) return sb.Append("ushort");
                if (type == typeof(byte)) return sb.Append("byte");
                if (type == typeof(sbyte)) return sb.Append("sbyte");
                if (type == typeof(float)) return sb.Append("float");
                if (type == typeof(double)) return sb.Append("double");
                if (type == typeof(decimal)) return sb.Append("decimal");
                if (type == typeof(string)) return sb.Append("string");
                if (type == typeof(void)) return sb.Append("void");
                if (type == typeof(bool)) return sb.Append("bool");

                if (type.IsGenericType && !type.IsGenericTypeDefinition)
                {
                    var def = type.GetGenericTypeDefinition();
                    var tick = def.Name.IndexOf('`');
                    sb.Append(tick >= 0 ? def.Name.Substring(0, tick) : def.Name);

                    var targs = type.GetGenericArguments();
                    sb = AppendTypeName(targs[0], sb.Append('<'));
                    for (int i = 1; i < targs.Length; i++)
                    {
                        sb = AppendTypeName(targs[i], sb.Append(','));
                    }
                    return sb.Append('>');
                }
                return sb.Append(type.Name);
            }
        }
    }
}
