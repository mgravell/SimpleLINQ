using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleLINQ.Transports
{
    /// <summary>
    /// Describes a general-purpose transport for performing operations against an underlying data-store
    /// </summary>
    public abstract partial class Transport
    {
        /// <summary>
        /// Synchronously execute a command
        /// </summary>
        public abstract object? Execute(ReadOnlySpan<object?> command);
        /// <summary>
        /// Asynchronously execute a command
        /// </summary>
        public virtual ValueTask<object?> ExecuteAsync(ReadOnlyMemory<object?> command, CancellationToken cancellationToken)
            => throw new NotSupportedException($"The underlying transport ('{GetType().FullName}') does not support asynchronous operations");

        /// <summary>Read values in a transport-specific way</summary>
        public virtual void CopyArrayTo(object value, Span<object?> target)
        {
            if (value is object[] arr)
            {
                arr.CopyTo(target);
            }
            else
            {
                throw new ArgumentException($"Unable to copy '{value?.GetType()?.Name}' data to an array", nameof(value));
            }
        }

        /// <summary>Read values in a transport-specific way</summary>
        public virtual object? GetArrayItem(object value, int index)
        {
            if (value is object[] arr) return arr[index];
            throw new ArgumentException($"Unable to get element by index of '{value?.GetType()?.Name}'", nameof(value));
        }
        /// <summary>Read values in a transport-specific way</summary>
        public virtual bool IsArray([NotNullWhen(true)] object? value, out int length)
        {
            if (value is object[] arr)
            {
                length = arr.Length;
                return true;
            }
            length = default;
            return false;
        }
        /// <summary>Read values in a transport-specific way</summary>
        public virtual int GetInt32(object? value)
            => Convert.ToInt32(value, CultureInfo.InvariantCulture);
        /// <summary>Read values in a transport-specific way</summary>
        public virtual long GetInt64(object? value)
            => Convert.ToInt64(value, CultureInfo.InvariantCulture);
        /// <summary>Read values in a transport-specific way</summary>
        public virtual string? GetString(object? value)
            => value is null ? null : Convert.ToString(value, CultureInfo.InvariantCulture);
        /// <summary>Read values in a transport-specific way</summary>
        public virtual float GetSingle(object? value)
            => Convert.ToSingle(value, CultureInfo.InvariantCulture);
        /// <summary>Read values in a transport-specific way</summary>
        public virtual double GetDouble(object? value)
            => Convert.ToDouble(value, CultureInfo.InvariantCulture);
        /// <summary>Read values in a transport-specific way</summary>
        public virtual DateTime GetDateTime(object? value)
        {
            var s = GetString(value) ?? throw new ArgumentNullException(nameof(value));
            return DateTime.ParseExact(s, s.Length > 10 && s[10] == 'T' ? "yyyy-MM-ddTHH:mm:ss.FFFFFFF" : "yyyy-MM-dd", CultureInfo.InvariantCulture);
        }
        /// <summary>Read values in a transport-specific way</summary>
        public virtual object? ChangeType(Type conversionType, object? value)
        {
            if (conversionType == typeof(int)) return GetInt32(value);
            if (conversionType == typeof(long)) return GetInt64(value);
            if (conversionType == typeof(string)) return GetString(value);
            if (conversionType == typeof(double)) return GetDouble(value);
            if (conversionType == typeof(float)) return GetSingle(value);
            if (conversionType == typeof(DateTime)) return GetDateTime(value);
            return conversionType == null ? value : Convert.ChangeType(value, conversionType, CultureInfo.InvariantCulture);
        }
    }
}
