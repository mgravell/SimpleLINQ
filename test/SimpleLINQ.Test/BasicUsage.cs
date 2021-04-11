using SimpleLINQ.Async;
using SimpleLINQ.Internal;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace SimpleLINQ.Test
{
    public class BasicUsage
    {
        static IQueryable<Foo> CreateQuery(int count = 0)
        {
            if (count == 0) return NullQueryProvider<Foo>.Default.CreateQuery<Foo>();

            var arr = new Foo[count];
            for (int i = 0; i < count; i++)
            {
                arr[i] = new Foo { Blap = i };
            }
            return new NullQueryProvider<Foo>(arr).CreateQuery<Foo>();
        }
        [Fact]
        public void CanConstructQuery()
        {
            IQueryable<Foo> query = CreateQuery();
            query = from x in query
                    where x.Bar == "abc" && x.Blap == 123
                    select x;
        }

        [Fact]
        public void CanConstructQueryManually()
        {
            IQueryable<Foo> query = CreateQuery();
            query = query.Where(x => x.Bar == "abc" && x.Blap == 123);
        }

        [Fact]
        public void CanComposeQueryManually()
        {
            IQueryable<Foo> query = CreateQuery();
            query = query.Where(x => x.Bar == "abc").Where(x => x.Blap == 123);
        }

        [Fact]
        public void CanComposeQueryManuallyWithSkipTakeAfter()
        {
            IQueryable<Foo> query = CreateQuery();
            query = query.Where(x => x.Bar == "abc").Where(x => x.Blap == 123).Skip(4).Skip(2).Take(42).Take(41).Take(43);

            var typed = Assert.IsAssignableFrom<Query>(query);
            Assert.Equal(6, typed.Skip);
            Assert.Equal(41, typed.Take);
        }

        [Fact]
        public void CannotFilterAfterSkip()
        {
            IQueryable<Foo> query = CreateQuery();
            var ex = Assert.Throws<InvalidOperationException>(() => query.Skip(2).Where(x => x.Bar == "abc"));
            Assert.Equal("Filters ('Where') cannot be added after row limits ('Skip'/'Take') have been applied", ex.Message);
        }


        [Fact]
        public void CannotFilterAfterTake()
        {
            IQueryable<Foo> query = CreateQuery();
            var ex = Assert.Throws<InvalidOperationException>(() => query.Take(2).Where(x => x.Bar == "abc"));
            Assert.Equal("Filters ('Where') cannot be added after row limits ('Skip'/'Take') have been applied", ex.Message);
        }

        [Fact]
        public void CanApplyCount()
        {
            IQueryable<Foo> query = CreateQuery();
            query = query.Where(x => x.Bar == "abc" && x.Blap == 123);
            query.Count();
        }

        [Fact]
        public void CanApplyCountWithPredicate()
        {
            IQueryable<Foo> query = CreateQuery();
            query.Count(x => x.Bar == "abc" && x.Blap == 123);
        }



        [Fact]
        public void CanApplyLongCount()
        {
            IQueryable<Foo> query = CreateQuery();
            query = query.Where(x => x.Bar == "abc" && x.Blap == 123);
            query.LongCount();
        }

        [Fact]
        public void CanApplyLongCountWithPredicate()
        {
            IQueryable<Foo> query = CreateQuery();
            query.LongCount(x => x.Bar == "abc" && x.Blap == 123);
        }



        [Fact]
        public void CanApplyFirst()
        {
            IQueryable<Foo> query = CreateQuery(2);
            query = query.Where(x => (x.Bar == "abc" && x.Blap == 123) || true);
            query.FirstOrDefault();
        }

        [Fact]
        public void CanApplyFirstWithPredicate()
        {
            IQueryable<Foo> query = CreateQuery(2);
            query.First(x => (x.Bar == "abc" && x.Blap == 123) || true);
        }

        [Fact]
        public void CanApplyFirstOrDefault()
        {
            IQueryable<Foo> query = CreateQuery();
            query = query.Where(x => x.Bar == "abc" && x.Blap == 123);
            query.FirstOrDefault();
        }

        [Fact]
        public void CanApplyFirstOrDefaultWithPredicate()
        {
            IQueryable<Foo> query = CreateQuery();
            query.FirstOrDefault(x => x.Bar == "abc" && x.Blap == 123);
        }



        [Fact]
        public void CanApplySingle()
        {
            IQueryable<Foo> query = CreateQuery(1);
            query = query.Where(x => (x.Bar == "abc" && x.Blap == 123) || true);
            query.Single();
        }

        [Fact]
        public void CanApplySingleWithPredicate()
        {
            IQueryable<Foo> query = CreateQuery(1);
            query.Single(x => (x.Bar == "abc" && x.Blap == 123) || true);
        }



        [Fact]
        public void CanApplySingleOrDefault()
        {
            IQueryable<Foo> query = CreateQuery();
            query = query.Where(x => x.Bar == "abc" && x.Blap == 123);
            query.SingleOrDefault();
        }

        [Fact]
        public void CanApplySingleOrDefaultWithPredicate()
        {
            IQueryable<Foo> query = CreateQuery();
            query.SingleOrDefault(x => x.Bar == "abc" && x.Blap == 123);
        }



        [Fact]
        public void CanCallToList()
        {
            IQueryable<Foo> query = CreateQuery();
            _ = query.Where(x => x.Bar == "abc" && x.Blap == 123).ToList();
        }


        [Fact]
        public void CanCallAny()
        {
            IQueryable<Foo> query = CreateQuery();
            query.Where(x => x.Bar == "abc" && x.Blap == 123).Any();
        }

        [Fact]
        public void CanCallAnyPredicate()
        {
            IQueryable<Foo> query = CreateQuery();
            query.Any(x => x.Bar == "abc" && x.Blap == 123);
        }



        [Fact]
        public void UseRegularLINQ()
        {
            var query = from x in CreateQuery()
                        where x.Bar == "abc" && x.Blap == 123
                        select x;
            Assert.False(query.Any());
        }

        [Fact]
        public void CanCallAllPredicate()
        {
            IQueryable<Foo> query = CreateQuery();
            query.All(x => x.Bar == "abc" && x.Blap == 123);
        }

        [Fact]
        public void CanCallElementAt()
        {
            IQueryable<Foo> query = CreateQuery(4);
            Assert.Equal(2, query.Select(x => x.Blap).ElementAt(2));

            Assert.Equal(0, query.Where(x => x.Blap > 99).Select(x => x.Blap).ElementAtOrDefault(2));
        }

        [Fact]
        public async Task CanCallElementAtAsync()
        {
            IQueryable<Foo> query = CreateQuery(4);
            Assert.Equal(2, await query.Select(x => x.Blap).ElementAtAsync(2));
            Assert.Equal(0, await query.Where(x => x.Blap > 99).Select(x => x.Blap).ElementAtOrDefaultAsync(2));
        }

        [Fact]
        public async Task CanCallElementAt_Index_Async()
        {
            IQueryable<Foo> query = CreateQuery(4).OrderBy(x => x.Blap);
            Assert.Equal(2, await query.Select(x => x.Blap).ElementAtAsync((Index)2));
            Assert.Equal(0, await query.Where(x => x.Blap > 99).Select(x => x.Blap).ElementAtOrDefaultAsync((Index)2));

            Assert.Equal(3, await query.Select(x => x.Blap).ElementAtAsync(new Index(1, true)));
            Assert.Equal(0, await query.OrderBy(x => x.Blap).Select(x => x.Blap).ElementAtOrDefaultAsync(new Index(0, true)));
        }

        [Fact]
        public void CanCallContains()
        {
            IQueryable<Foo> query = CreateQuery(4);
            Assert.True(query.Select(x => x.Blap).Contains(2));
            Assert.False(query.Select(x => x.Blap).Contains(7));
        }



        public class Foo
        {
            public string Bar { get; set; }

            public int Blap { get; set; }
        }
    }
}


