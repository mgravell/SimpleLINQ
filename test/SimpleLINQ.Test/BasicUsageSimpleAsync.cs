using SimpleLINQ.Async;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace SimpleLINQ.Test
{
    public class BasicUsageSimpleAsync
    {
        static IQueryable<Foo> CreateQuery(int count = 0)
        {
            if (count == 0) return NullQueryProvider<Foo>.Default.CreateQuery<Foo>();

            var arr = new Foo[count];
            for (int i = 0; i < count; i++)
            {
                arr[i] = new Foo();
            }
            return new NullQueryProvider<Foo>(arr).CreateQuery<Foo>();
        }

        [Fact]
        public async Task CanApplyCountAsync()
        {
            IQueryable<Foo> query = CreateQuery();
            query = query.Where(x => x.Bar == "abc" && x.Blap == 123);
            await query.CountAsync();
        }

        [Fact]
        public async Task CanApplyCountWithPredicateAsync()
        {
            IQueryable<Foo> query = CreateQuery();
            await query.CountAsync(x => x.Bar == "abc" && x.Blap == 123);
        }

        [Fact]
        public async Task CanApplyLongCountAsync()
        {
            IQueryable<Foo> query = CreateQuery();
            query = query.Where(x => x.Bar == "abc" && x.Blap == 123);
            await query.LongCountAsync();
        }

        [Fact]
        public async Task CanApplyLongCountWithPredicateAsync()
        {
            IQueryable<Foo> query = CreateQuery();
            await query.LongCountAsync(x => x.Bar == "abc" && x.Blap == 123);
        }

        [Fact]
        public async Task CanApplyFirstOrDefaultAsync()
        {
            IQueryable<Foo> query = CreateQuery();
            query = query.Where(x => x.Bar == "abc" && x.Blap == 123);
            await query.FirstOrDefaultAsync();
        }

        [Fact]
        public async Task CanApplyFirstOrDefaultWithPredicateAsync()
        {
            IQueryable<Foo> query = CreateQuery();
            await query.FirstOrDefaultAsync(x => x.Bar == "abc" && x.Blap == 123);
        }

        [Fact]
        public async Task CanApplyFirstAsync()
        {
            IQueryable<Foo> query = CreateQuery(2);
            query = query.Where(x => (x.Bar == "abc" && x.Blap == 123) || true);
            await query.FirstAsync();
        }

        [Fact]
        public async Task CanApplyFirstWithPredicateAsync()
        {
            IQueryable<Foo> query = CreateQuery(2);
            await query.FirstAsync(x => (x.Bar == "abc" && x.Blap == 123) || true);
        }

        [Fact]
        public async Task CanApplySingleAsync()
        {
            IQueryable<Foo> query = CreateQuery(1);
            query = query.Where(x => (x.Bar == "abc" && x.Blap == 123) || true);
            await query.SingleAsync();
        }

        [Fact]
        public async Task CanApplySingleWithPredicateAsync()
        {
            IQueryable<Foo> query = CreateQuery(1);
            await query.SingleAsync(x => (x.Bar == "abc" && x.Blap == 123) || true);
        }

        [Fact]
        public async Task CanApplySingleOrDefaultAsync()
        {
            IQueryable<Foo> query = CreateQuery();
            query = query.Where(x => x.Bar == "abc" && x.Blap == 123);
            await query.SingleOrDefaultAsync();
        }

        [Fact]
        public async Task CanApplySingleOrDefaultWithPredicateAsync()
        {
            IQueryable<Foo> query = CreateQuery();
            await query.SingleOrDefaultAsync(x => x.Bar == "abc" && x.Blap == 123);
        }

        [Fact]
        public async Task CanCallAnyAsync()
        {
            IQueryable<Foo> query = CreateQuery();
            await query.Where(x => x.Bar == "abc" && x.Blap == 123).AnyAsync();
        }

        [Fact]
        public async Task CanCallAnyPredicateAsync()
        {
            IQueryable<Foo> query = CreateQuery();
            await query.AnyAsync(x => x.Bar == "abc" && x.Blap == 123);
        }

        [Fact]
        public async Task CanCallToListAsync()
        {
            IQueryable<Foo> query = CreateQuery();
            await query.Where(x => x.Bar == "abc" && x.Blap == 123).ToListAsync();
        }

        [Fact]
        public async Task CanCallAllPredicateAsync()
        {
            IQueryable<Foo> query = CreateQuery();
            await query.AllAsync(x => x.Bar == "abc" && x.Blap == 123);
        }


        public class Foo
        {
            public string Bar { get; set; }

            public int Blap { get; set; }
        }
    }
}

