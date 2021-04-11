using System;
using System.Linq.Expressions;
using Xunit;
using Xunit.Abstractions;

namespace SimpleLINQ.Test
{
    public class ProjectionUnitTests
    {
        private readonly ITestOutputHelper _log;

        public ProjectionUnitTests(ITestOutputHelper log) => _log = log;
        private string Log(string message)
        {
            _log?.WriteLine(message);
            return message;
        }
        public class Root
        {
            public int A { get; set; }
            public int B { get; set; }
            public DateTime C { get; set; }
        }

        static Expression<Func<Root, T>> Project<T>(Expression<Func<Root, T>> lambda) => lambda;
        static Expression<Func<TB, TC>> GetSecond<TA, TB, TC>(Expression<Func<TA, TB>> _, Expression<Func<TB, TC>> second) => second;

        


        [Fact]
        public void CanApplyProjectionFromObjToObj()
        {
            var first = Project(x => new Root { A = 2 * x.A, B = x.B + 6, C = x.C.AddDays(1) });
            Assert.Equal(@"x => new Root() {A = (2 * x.A), B = (x.B + 6), C = x.C.AddDays(1)}", Log(first.ToString()));
            var second = GetSecond(first, y => new Root { A = y.A * 3, B = 4 + y.B, C = y.C.AddDays(2) });
            var merged = ExpressionUtils.Merge(first, second);
            Assert.Equal(Assert.Single(first.Parameters).Type, Assert.Single(merged.Parameters).Type);
            Assert.Equal(@"x => new Root() {A = (x.A * 6), B = (x.B + 10), C = x.C.AddDays(1).AddDays(2)}", Log(merged.ToString()));
        }

        [Fact]
        public void CanApplyProjectionFromObjToObjReversed()
        {
            var first = Project(x => new Root { A = x.A * 2, B = 6 + x.B, C = x.C.AddDays(1) });
            Assert.Equal(@"x => new Root() {A = (x.A * 2), B = (6 + x.B), C = x.C.AddDays(1)}", Log(first.ToString()));
            var second = GetSecond(first, y => new Root { A = 3 * y.A, B = y.B + 4, C = y.C.AddDays(2) });
            var merged = ExpressionUtils.Merge(first, second);
            Assert.Equal(Assert.Single(first.Parameters).Type, Assert.Single(merged.Parameters).Type);
            Assert.Equal(@"x => new Root() {A = (x.A * 6), B = (x.B + 10), C = x.C.AddDays(1).AddDays(2)}", Log(merged.ToString()));
        }

        [Fact]
        public void CanApplyProjectionFromAnonToAnon()
        {
            var first = Project(x => new { A = 2 * x.A, B = x.B + 6, C = x.C.AddDays(1) });
            Assert.Equal(@"x => new #(A = (2 * x.A), B = (x.B + 6), C = x.C.AddDays(1))", Log(StripType(first)));
            var second = GetSecond(first, y => new { A = y.A * 3, B = 4 + y.B, C = y.C.AddDays(2) });
            var merged = ExpressionUtils.Merge(first, second);
            Assert.Equal(Assert.Single(first.Parameters).Type, Assert.Single(merged.Parameters).Type);
            Assert.Equal(@"x => new #(A = (x.A * 6), B = (x.B + 10), C = x.C.AddDays(1).AddDays(2))", Log(StripType(merged)));
        }
        static string StripType(LambdaExpression expression)
        {
            var s = expression.ToString();
            return s.Replace(expression.Body.Type.Name, "#");
        }
        [Fact]
        public void CanApplyProjectionFromAnonToAnonReversed()
        {
            var first = Project(x => new { A = x.A * 2, B = 6 + x.B, C = x.C.AddDays(1) });
            Assert.Equal(@"x => new #(A = (x.A * 2), B = (6 + x.B), C = x.C.AddDays(1))", Log(StripType(first)));
            var second = GetSecond(first, y => new { A = 3 * y.A, B = y.B + 4, C = y.C.AddDays(2) });
            var merged = ExpressionUtils.Merge(first, second);
            Assert.Equal(Assert.Single(first.Parameters).Type, Assert.Single(merged.Parameters).Type);
            Assert.Equal(@"x => new #(A = (x.A * 6), B = (x.B + 10), C = x.C.AddDays(1).AddDays(2))", Log(StripType(merged)));
        }
    }
}
