using SimpleLINQ.Internal;
using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;

namespace SimpleLINQ
{
    /// <summary>
    /// Utility methods for working with LINQ expressions
    /// </summary>
    public static class ExpressionUtils
    {
        /// <summary>
        /// Performs the logical (boolean) negation of a lambda expression
        /// </summary>
        public static LambdaExpression Negate(this LambdaExpression lambda)
        {
            Expression body;
            if (lambda.Body is UnaryExpression ue && ue.NodeType == ExpressionType.Not && (ue.Type == typeof(bool) || ue.Type == typeof(bool?)))
            {
                body = ue.Operand; // unwrap '!x' to 'x'
            }
            else
            {
                body = Expression.Not(lambda.Body); // wrap 'x' to '!x'
            }
            return Expression.Lambda(body, lambda.TailCall, lambda.Parameters);
        }

        /// <summary>
        /// Simplifies a lambda expression, combining simple operations and inlining nested lambda invocations
        /// </summary>
        [return: NotNullIfNotNull("expression")]
        public static LambdaExpression? Simplify(this LambdaExpression? expression)
            => (LambdaExpression?)OperatorCombiningExpressionVisitor.Simplify(expression);

        /// <summary>
        /// Simplifies a lambda expression, combining simple operations and inlining nested lambda invocations
        /// </summary>
        [return: NotNullIfNotNull("lambda")]
        public static Expression<TDelegate>? Simplify<TDelegate>(this Expression<TDelegate>? lambda)
            => (Expression<TDelegate>?)OperatorCombiningExpressionVisitor.Simplify(lambda);

        /// <summary>
        /// Indicates whether a lambda expression is trivial, i.e. <c>x => x</c>
        /// </summary>
        public static bool IsIdentity(this LambdaExpression lambda)
        {
            if (lambda is not null && lambda.Parameters.Count == 1) // looking for simple unadorned x => x
            {
                var p = lambda.Parameters[0];
                var body = lambda.Body;
                return ReferenceEquals(body, p) && p.Type == body.Type;
            }
            return false;
        }

        /// <summary>
        /// Converts A=>B and B=>C to A=>C
        /// </summary>
        public static LambdaExpression Merge(this LambdaExpression first, LambdaExpression second)
        {
            if (first is null) throw new ArgumentNullException(nameof(first));
            if (second is null || second.IsIdentity()) return first;

            ParameterExpression p;
            if (second.Parameters.Count != 1 || (p = second.Parameters[0]).Type != first.Body.Type)
                throw new ArgumentException("To merge expressions, the second expression must take a single parameter, of the type yielded by the first expression.", nameof(second));

            try
            {
                var merged = new ExpressionMergeVisitor(first, p).Visit(second.Body);
                return Expression.Lambda(merged, first.Parameters);
            }
            catch (Exception ex)
            {
                throw new NotSupportedException($"It was not possible to merge the supplied expressions", ex);
            }
        }

        /// <summary>
        /// Converts A=>B and B=>C to A=>C
        /// </summary>
        public static Expression<Func<TFirst, TThird>> Merge<TFirst, TSecond, TThird>(this Expression<Func<TFirst, TSecond>> first, Expression<Func<TSecond, TThird>> second)
            => (Expression<Func<TFirst, TThird>>)(Expression)Merge((LambdaExpression)first, (LambdaExpression)second);

        /// <summary>
        /// Attempt to parse an expression at runtime, not including captured variables or other changeable expressions
        /// </summary>
        public static bool TryGetPureConstantValue(this Expression expression, out object? value)
            => TryGetConstantValue(expression, out value, out var pure) && pure;

        /// <summary>
        /// Attempt to parse an expression at runtime, also indicating whether it involves captured variables or other changeable expressions, or whether it is a fixed/pure constant
        /// </summary>
        public static bool TryGetConstantValue(this Expression expression, out object? value, out bool pureConstant)
        {
            value = default;
#pragma warning disable IDE0018 // much cleaner with just the one set of locals
            object? left, right;
            bool leftPure, rightPure;
#pragma warning restore IDE0018
            switch (expression)
            {
                case null:
                    value = null;
                    pureConstant = true;
                    return true;
                case ConstantExpression ce:
                    value = ce.Value;
                    pureConstant = true;
                    return true;
                case MemberExpression me when TryGetConstantValue(me.Expression, out left, out _):
                    switch (me.Member)
                    {
                        case PropertyInfo property:
                            value = property.GetValue(left);
                            pureConstant = false;
                            return true;
                        case FieldInfo field:
                            value = field.GetValue(left);
                            pureConstant = (field.IsStatic && field.IsInitOnly) || field.IsLiteral;
                            return true;
                    }
                    break;
                case UnaryExpression ue when ue.Method is null && TryGetConstantValue(ue.Operand, out left, out pureConstant):
                    switch (ue.NodeType)
                    {
                        case ExpressionType.IsFalse:
                            switch (left)
                            {
                                case bool b:
                                    value = b == false;
                                    return true;
                            }
                            break;
                        case ExpressionType.IsTrue:
                            switch (left)
                            {
                                case bool b:
                                    value = b == true;
                                    return true;
                            }
                            break;
                        case ExpressionType.Negate:
                            switch (left)
                            {
                                case int i:
                                    value = -i;
                                    return true;
                                case long l:
                                    value = -l;
                                    return true;
                                case float f:
                                    value = -f;
                                    return true;
                                case double d:
                                    value = -d;
                                    return true;
                            }
                            break;
                        case ExpressionType.UnaryPlus:
                            value = left;
                            return true;
                        case ExpressionType.NegateChecked:
                            checked
                            {
                                switch (left)
                                {
                                    case int i:
                                        value = -i;
                                        return true;
                                    case long l:
                                        value = -l;
                                        return true;
                                    case float f:
                                        value = -f;
                                        return true;
                                    case double d:
                                        value = -d;
                                        return true;
                                }
                            }
                            break;
                        case ExpressionType.Not:
                            switch (left)
                            {
                                case int i:
                                    value = ~i;
                                    return true;
                                case long l:
                                    value = ~l;
                                    return true;
                                case bool b:
                                    value = !b;
                                    return true;
                            }
                            break;
                        case ExpressionType.Convert:
                            var nt = Nullable.GetUnderlyingType(ue.Type);
                            if (nt is not null && (left is null || left.GetType() == nt))
                            {
                                // e.g. trying to convert int => int? - nothing to do in reflection land
                                value = left;
                                return true;
                            }
                            try
                            {
                                value = Convert.ChangeType(left, ue.Type, CultureInfo.InvariantCulture);
                                return true;
                            }
                            catch { }
                            break;
                    }
                    break;
                case MethodCallExpression mce when TryGetConstantValue(mce.Object, out left, out _)
                    && TryGetArgs(mce.Arguments, out var args):
                    try
                    {
                        value = mce.Method.Invoke(left, args);
                        pureConstant = false;
                        return true;
                    }
                    catch { }
                    break;
                case ConditionalExpression ce when TryGetConstantValue(ce.Test, out left, out leftPure) && left is bool testResult
                    && TryGetConstantValue(testResult ? ce.IfTrue : ce.IfFalse, out right, out rightPure):
                    pureConstant = leftPure & rightPure;
                    value = right;
                    return true;
                case BinaryExpression be when (be.Method is null || be.Method.DeclaringType == typeof(string) && be.Method.Name == nameof(string.Concat)) && be.Conversion is null:
                    switch (be.NodeType)
                    {
                        // these ones re-use some logic that is also used when simplifying operators
                        case ExpressionType.MultiplyChecked:
                        case ExpressionType.Multiply:
                        case ExpressionType.Add:
                        case ExpressionType.AddChecked:
                            if (TryGetConstantValue(be.Left, out left, out leftPure) && TryGetConstantValue(be.Right, out right, out rightPure)
                                && TryExecuteBinary(be.NodeType, left, right, out value))
                            {
                                pureConstant = leftPure & rightPure;
                                return true;
                            }
                            break;
                        case ExpressionType.Subtract when TryGetConstantValue(be.Left, out left, out leftPure) && TryGetConstantValue(be.Right, out right, out rightPure):
                            pureConstant = leftPure & rightPure;
                            switch (left)
                            {
                                case int il when right is int ir:
                                    value = il + ir;
                                    return true;
                                case long ll when right is long lr:
                                    value = ll + lr;
                                    return true;
                                case float fl when right is float fr:
                                    value = fl + fr;
                                    return true;
                                case double dl when right is double dr:
                                    value = dl + dr;
                                    return true;
                            }
                            break;
                        case ExpressionType.SubtractChecked when TryGetConstantValue(be.Left, out left, out leftPure) && TryGetConstantValue(be.Right, out right, out rightPure):
                            pureConstant = leftPure & rightPure;
                            checked
                            {
                                switch (left)
                                {
                                    case int il when right is int ir:
                                        value = il - ir;
                                        return true;
                                    case long ll when right is long lr:
                                        value = ll - lr;
                                        return true;
                                    case float fl when right is float fr:
                                        value = fl - fr;
                                        return true;
                                    case double dl when right is double dr:
                                        value = dl - dr;
                                        return true;
                                }
                            }
                            break;
                        case ExpressionType.Divide when TryGetConstantValue(be.Left, out left, out leftPure) && TryGetConstantValue(be.Right, out right, out rightPure):
                            pureConstant = leftPure & rightPure;
                            switch (left)
                            {
                                case int il when right is int ir:
                                    value = il / ir;
                                    return true;
                                case long ll when right is long lr:
                                    value = ll / lr;
                                    return true;
                                case float fl when right is float fr:
                                    value = fl / fr;
                                    return true;
                                case double dl when right is double dr:
                                    value = dl / dr;
                                    return true;
                            }
                            break;
                        case ExpressionType.Modulo when TryGetConstantValue(be.Left, out left, out leftPure) && TryGetConstantValue(be.Right, out right, out rightPure):
                            pureConstant = leftPure & rightPure;
                            switch (left)
                            {
                                case int il when right is int ir:
                                    value = il % ir;
                                    return true;
                                case long ll when right is long lr:
                                    value = ll % lr;
                                    return true;
                                case float fl when right is float fr:
                                    value = fl % fr;
                                    return true;
                                case double dl when right is double dr:
                                    value = dl % dr;
                                    return true;
                            }
                            break;
                        case ExpressionType.LeftShift when TryGetConstantValue(be.Left, out left, out leftPure) && TryGetConstantValue(be.Right, out right, out rightPure):
                            pureConstant = leftPure & rightPure;
                            switch (left)
                            {
                                case int il when right is int shift:
                                    value = il << shift;
                                    return true;
                                case long ll when right is int shift:
                                    value = ll << shift;
                                    return true;
                            }
                            break;
                        case ExpressionType.RightShift when TryGetConstantValue(be.Left, out left, out leftPure) && TryGetConstantValue(be.Right, out right, out rightPure):
                            pureConstant = leftPure & rightPure;
                            switch (left)
                            {
                                case int il when right is int shift:
                                    value = il >> shift;
                                    return true;
                                case long ll when right is int shift:
                                    value = ll >> shift;
                                    return true;
                            }
                            break;
                        case ExpressionType.Equal when TryGetConstantValue(be.Left, out left, out leftPure) && TryGetConstantValue(be.Right, out right, out rightPure):
                            pureConstant = leftPure & rightPure;
                            value = Equals(left, right);
                            return true;
                        case ExpressionType.NotEqual when TryGetConstantValue(be.Left, out left, out leftPure) && TryGetConstantValue(be.Right, out right, out rightPure):
                            pureConstant = leftPure & rightPure;
                            value = !Equals(left, right);
                            return true;
                        case ExpressionType.GreaterThan when TryGetConstantValue(be.Left, out left, out leftPure) && TryGetConstantValue(be.Right, out right, out rightPure):
                            pureConstant = leftPure & rightPure;
                            switch (left)
                            {
                                case int il when right is int ir:
                                    value = il > ir;
                                    return true;
                                case long ll when right is long lr:
                                    value = ll > lr;
                                    return true;
                                case float fl when right is float fr:
                                    value = fl > fr;
                                    return true;
                                case double dl when right is double dr:
                                    value = dl > dr;
                                    return true;
                            }
                            break;
                        case ExpressionType.GreaterThanOrEqual when TryGetConstantValue(be.Left, out left, out leftPure) && TryGetConstantValue(be.Right, out right, out rightPure):
                            pureConstant = leftPure & rightPure;
                            switch (left)
                            {
                                case int il when right is int ir:
                                    value = il >= ir;
                                    return true;
                                case long ll when right is long lr:
                                    value = ll >= lr;
                                    return true;
                                case float fl when right is float fr:
                                    value = fl >= fr;
                                    return true;
                                case double dl when right is double dr:
                                    value = dl >= dr;
                                    return true;
                            }
                            break;
                        case ExpressionType.LessThan when TryGetConstantValue(be.Left, out left, out leftPure) && TryGetConstantValue(be.Right, out right, out rightPure):
                            pureConstant = leftPure & rightPure;
                            switch (left)
                            {
                                case int il when right is int ir:
                                    value = il < ir;
                                    return true;
                                case long ll when right is long lr:
                                    value = ll < lr;
                                    return true;
                                case float fl when right is float fr:
                                    value = fl < fr;
                                    return true;
                                case double dl when right is double dr:
                                    value = dl < dr;
                                    return true;
                            }
                            break;
                        case ExpressionType.LessThanOrEqual when TryGetConstantValue(be.Left, out left, out leftPure) && TryGetConstantValue(be.Right, out right, out rightPure):
                            pureConstant = leftPure & rightPure;
                            switch (left)
                            {
                                case int il when right is int ir:
                                    value = il <= ir;
                                    return true;
                                case long ll when right is long lr:
                                    value = ll <= lr;
                                    return true;
                                case float fl when right is float fr:
                                    value = fl <= fr;
                                    return true;
                                case double dl when right is double dr:
                                    value = dl <= dr;
                                    return true;
                            }
                            break;
                        case ExpressionType.And when TryGetConstantValue(be.Left, out left, out leftPure) && TryGetConstantValue(be.Right, out right, out rightPure):
                            pureConstant = leftPure & rightPure;
                            switch (left)
                            {
                                case int il when right is int ir:
                                    value = il & ir;
                                    return true;
                                case long ll when right is long lr:
                                    value = ll & lr;
                                    return true;
                                case bool bl when right is bool br:
                                    value = bl & br;
                                    return true;
                            }
                            break;
                        case ExpressionType.Or when TryGetConstantValue(be.Left, out left, out leftPure) && TryGetConstantValue(be.Right, out right, out rightPure):
                            pureConstant = leftPure & rightPure;
                            switch (left)
                            {
                                case int il when right is int ir:
                                    value = il | ir;
                                    return true;
                                case long ll when right is long lr:
                                    value = ll | lr;
                                    return true;
                                case bool bl when right is bool br:
                                    value = bl | br;
                                    return true;
                            }
                            break;
                        case ExpressionType.AndAlso when TryGetConstantValue(be.Left, out left, out leftPure):
                            switch (left)
                            {
                                case bool bl:
                                    if (bl)
                                    {
                                        if (TryGetConstantValue(be.Right, out right, out rightPure) && right is bool br)
                                        {
                                            pureConstant = leftPure & rightPure;
                                            value = br;
                                            return true;
                                        }
                                    }
                                    else
                                    {
                                        pureConstant = true;
                                        value = false;
                                        return true;
                                    }
                                    break;
                            }
                            break;
                        case ExpressionType.OrElse when TryGetConstantValue(be.Left, out left, out leftPure):
                            switch (left)
                            {
                                case bool bl:
                                    if (bl)
                                    {
                                        pureConstant = true;
                                        value = true;
                                        return true;
                                    }
                                    else
                                    {
                                        if (TryGetConstantValue(be.Right, out right, out rightPure) && right is bool br)
                                        {
                                            pureConstant = leftPure & rightPure;
                                            value = br;
                                            return true;
                                        }
                                    }
                                    break;
                            }
                            break;
                    }
                    break;
                case NewExpression ne when TryGetArgs(ne.Arguments, out var args):
                    try
                    {
                        value = ne.Constructor.Invoke(args);
                        pureConstant = false;
                        return true;
                    }
                    catch { }
                    break;
            }
            pureConstant = false;
            return false;

            static bool TryGetArgs(ReadOnlyCollection<Expression> arguments, out object?[] result)
            {
                if (arguments.Count == 0)
                {
                    result = Array.Empty<object?>();
                    return true;
                }
                result = new object?[arguments.Count];
                for (int i = 0; i < result.Length; i++)
                {
                    if (!TryGetConstantValue(arguments[i], out result[i], out _))
                        return false; // need all args locally, else: nope
                }
                return true;
            }
        }

        internal static bool TryExecuteBinary(ExpressionType op, object? left, object? right, out object? value)
        {
            try
            {
                switch (op)
                {
                    case ExpressionType.Multiply:
                        switch (left)
                        {
                            case int il when right is int ir:
                                value = il * ir;
                                return true;
                            case long ll when right is long lr:
                                value = ll * lr;
                                return true;
                            case float fl when right is float fr:
                                value = fl * fr;
                                return true;
                            case double dl when right is double dr:
                                value = dl * dr;
                                return true;
                        }
                        break;
                    case ExpressionType.MultiplyChecked:
                        checked
                        {
                            switch (left)
                            {
                                case int il when right is int ir:
                                    value = il * ir;
                                    return true;
                                case long ll when right is long lr:
                                    value = ll * lr;
                                    return true;
                                case float fl when right is float fr:
                                    value = fl * fr;
                                    return true;
                                case double dl when right is double dr:
                                    value = dl * dr;
                                    return true;
                            }
                        }
                        break;
                    case ExpressionType.Add:
                        switch (left)
                        {
                            case int il when right is int ir:
                                value = il + ir;
                                return true;
                            case long ll when right is long lr:
                                value = ll + lr;
                                return true;
                            case float fl when right is float fr:
                                value = fl + fr;
                                return true;
                            case double dl when right is double dr:
                                value = dl + dr;
                                return true;
                            case string sl:
                                switch (right)
                                {
                                    case string sr:
                                        value = sl + sr;
                                        return true;
                                    case null:
                                        value = sl;
                                        return true;

                                }
                                break;
                            case null:
                                switch (right)
                                {
                                    case string sr:
                                        value = sr;
                                        return true;
                                    case null:
                                        value = null;
                                        return true;
                                }
                                break;
                        }
                        break;
                    case ExpressionType.AddChecked:
                        checked
                        {
                            switch (left)
                            {
                                case int il when right is int ir:
                                    value = il + ir;
                                    return true;
                                case long ll when right is long lr:
                                    value = ll + lr;
                                    return true;
                                case float fl when right is float fr:
                                    value = fl + fr;
                                    return true;
                                case double dl when right is double dr:
                                    value = dl + dr;
                                    return true;
                            }
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            value = default;
            return false;
        }

        private sealed class ExpressionMergeVisitor : OperatorCombiningExpressionVisitor
        {
            private readonly LambdaExpression _first;
            private readonly ParameterExpression _p;
            public ExpressionMergeVisitor(LambdaExpression root, ParameterExpression p)
            {
                _first = root;
                _p = p;
            }
            protected override Expression? VisitParameter(ParameterExpression node)
                => node == _p ? _first.Body : base.Visit(node);

            protected override Expression VisitMember(MemberExpression node)
            {
                var target = node.Expression;
                if (target != _p) target = Visit(target);
                Expression? expr;
                switch (target)
                {
                    case ParameterExpression when target == _p:
                        var hunt = node.Member;
                        switch (_first.Body)
                        {
                            case NewExpression ne:
                                if (TryResolve(hunt, ne, out expr))
                                    return expr;
                                break;
                            case MemberInitExpression mie:
                                foreach (var binding in mie.Bindings)
                                {
                                    if (binding.Member == hunt && binding is MemberAssignment ma)
                                        return ma.Expression;
                                }
                                if (TryResolve(hunt, mie.NewExpression, out expr))
                                    return expr;
                                break;
                        }
                        break;
                    case NewExpression ne: // transparent identifiers!
                        if (TryResolve(node.Member, ne, out expr))
                            return expr;
                        break;
                }

                return base.VisitMember(node);
            }

            static bool TryResolve(MemberInfo member, NewExpression ne, [NotNullWhen(true)] out Expression? expression)
            {
                if (ne.Arguments.Count != 0)
                {
                    if (ne.Members is not null)
                    {
                        int index = 0;
                        foreach (var candidate in ne.Members)
                        {
                            if (candidate == member)
                            {
                                expression = ne.Arguments[index];
                                return true;
                            }
                            index++;
                        }
                    }
                    // fallback; match by name from constructor to member; not perfect, but better than nothing
                    if (ne.Constructor is not null)
                    {
                        var members = GetSpoofedMembers(ne.Constructor);
                        int index = 0;
                        foreach (var candidate in members)
                        {
                            if (candidate == member)
                            {
                                expression = ne.Arguments[index];
                                return true;
                            }
                            index++;
                        }
                    }
                }
                expression = default;
                return false;
            }

            static MemberInfo?[] GetSpoofedMembers(ConstructorInfo constructor)
            {
                return s_SpoofedMembers.TryGetValue(constructor, out var value) ? value : SpoofAndAdd(constructor);

                static MemberInfo?[] SpoofAndAdd(ConstructorInfo constructor)
                {
                    var result = Spoof(constructor);
                    s_SpoofedMembers[constructor] = result;
                    return result;
                }
                static MemberInfo?[] Spoof(ConstructorInfo constructor)
                {
                    var dt = constructor.DeclaringType;
                    if (dt is null) return Array.Empty<MemberInfo?>();
                    var args = constructor.GetParameters();

                    if (args.Length == 0) return Array.Empty<MemberInfo?>();

                    var props = dt.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                    var fields = dt.GetFields(BindingFlags.Public | BindingFlags.Instance);

                    MemberInfo?[] result = new MemberInfo?[args.Length];
                    for (int i = 0; i < args.Length; i++)
                    {
                        result[i] = TryFind(props, fields, args[i].Name, args[i].ParameterType);
                    }
                    return result;
                }
                static MemberInfo? TryFind(PropertyInfo[] props, FieldInfo[] fields, string? name, Type type)
                {
                    foreach (var test in props)
                    {
                        if (test.Name == name && test.PropertyType == type) return test;
                    }
                    foreach (var test in fields)
                    {
                        if (test.Name == name && test.FieldType == type) return test;
                    }
                    foreach (var test in props)
                    {
                        if (string.Equals(test.Name, name, StringComparison.InvariantCultureIgnoreCase) && test.PropertyType == type) return test;
                    }
                    foreach (var test in fields)
                    {
                        if (string.Equals(test.Name, name, StringComparison.InvariantCultureIgnoreCase) && test.FieldType == type) return test;
                    }
                    return null;
                }

            }
            static readonly ConcurrentDictionary<ConstructorInfo, MemberInfo?[]> s_SpoofedMembers = new();
        }
    }
}
