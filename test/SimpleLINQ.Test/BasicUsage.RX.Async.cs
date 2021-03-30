using SimpleLINQ.Async;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace SimpleLINQ.Test
{
    public class BasicUsageRXAsync
    {
        static IAsyncQueryable<Foo> CreateQuery(int count = 0)
        {
            if (count == 0) return NullQueryProvider<Foo>.Default.CreateQuery<Foo>().AsAsyncQueryable();

            var arr = new Foo[count];
            for (int i = 0; i < count; i++)
            {
                arr[i] = new Foo();
            }
            return new NullQueryProvider<Foo>(arr).CreateQuery<Foo>().AsAsyncQueryable();
        }

        [Fact]
        public async Task CanApplyCountAsync()
        {
            var query = CreateQuery();
            query = query.Where(x => x.Bar == "abc" && x.Blap == 123);
            await query.CountAsync();
        }

        [Fact]
        public async Task CanApplyCountWithPredicateAsync()
        {
            var query = CreateQuery();
            await query.CountAsync(x => x.Bar == "abc" && x.Blap == 123);
        }

        [Fact]
        public async Task CanApplyLongCountAsync()
        {
            var query = CreateQuery();
            query = query.Where(x => x.Bar == "abc" && x.Blap == 123);
            await query.LongCountAsync();
        }

        [Fact]
        public async Task CanApplyLongCountWithPredicateAsync()
        {
            var query = CreateQuery();
            await query.LongCountAsync(x => x.Bar == "abc" && x.Blap == 123);
        }

        [Fact]
        public async Task CanApplyFirstOrDefaultAsync()
        {
            var query = CreateQuery();
            query = query.Where(x => x.Bar == "abc" && x.Blap == 123);
            await query.FirstOrDefaultAsync();
        }

        [Fact]
        public async Task CanApplyFirstOrDefaultWithPredicateAsync()
        {
            var query = CreateQuery();
            await query.FirstOrDefaultAsync(x => x.Bar == "abc" && x.Blap == 123);
        }

        [Fact]
        public async Task CanApplyFirstAsync()
        {
            var query = CreateQuery(2);
            query = query.Where(x => (x.Bar == "abc" && x.Blap == 123) || true);
            await query.FirstAsync();
        }

        [Fact]
        public async Task CanApplyFirstWithPredicateAsync()
        {
            var query = CreateQuery(2);
            await query.FirstAsync(x => (x.Bar == "abc" && x.Blap == 123) || true);
        }

        [Fact]
        public async Task CanApplySingleAsync()
        {
            var query = CreateQuery(1);
            query = query.Where(x => (x.Bar == "abc" && x.Blap == 123) || true);
            await query.SingleAsync();
        }

        [Fact]
        public async Task CanApplySingleWithPredicateAsync()
        {
            var query = CreateQuery(1);
            await query.SingleAsync(x => (x.Bar == "abc" && x.Blap == 123) || true);
        }

        [Fact]
        public async Task CanApplySingleOrDefaultAsync()
        {
            var query = CreateQuery();
            query = query.Where(x => x.Bar == "abc" && x.Blap == 123);
            await query.SingleOrDefaultAsync();
        }

        [Fact]
        public async Task CanApplySingleOrDefaultWithPredicateAsync()
        {
            var query = CreateQuery();
            await query.SingleOrDefaultAsync(x => x.Bar == "abc" && x.Blap == 123);
        }

        [Fact]
        public async Task CanCallAnyAsync()
        {
            var query = CreateQuery();
            await query.Where(x => x.Bar == "abc" && x.Blap == 123).AnyAsync();
        }

        [Fact]
        public async Task CanCallAnyPredicateAsync()
        {
            var query = CreateQuery();
            await query.AnyAsync(x => x.Bar == "abc" && x.Blap == 123);
        }

        [Fact]
        public async Task CanCallToListAsync()
        {
            var query = CreateQuery();
            await query.Where(x => x.Bar == "abc" && x.Blap == 123).ToListAsync();
        }

        [Fact]
        public async Task CanCallToArrayAsync()
        {
            var query = CreateQuery();
            await query.Where(x => x.Bar == "abc" && x.Blap == 123).ToArrayAsync();
        }

        [Fact]
        public async Task CanCallAllPredicateAsync()
        {
            var query = CreateQuery();
            await query.AllAsync(x => x.Bar == "abc" && x.Blap == 123);
        }


        public class Foo
        {
            public string Bar { get; set; }

            public int Blap { get; set; }
        }
    }
}

