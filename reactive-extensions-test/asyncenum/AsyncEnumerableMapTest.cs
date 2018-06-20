using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Threading.Tasks;

namespace akarnokd.reactive_extensions_test.asyncenum
{
    [TestFixture]
    public class AsyncEnumerableMapTest
    {
        [Test]
        public async Task Basic()
        {
            var source = AsyncEnumerable.Range(1, 5)
                .Map(v => v.ToString());

            (await source.TestAsync())
                .AssertResult("1", "2", "3", "4", "5");
        }

        [Test]
        public async Task Empty()
        {
            var source = AsyncEnumerable.Empty<int>()
                .Map(v => v.ToString());

            (await source.TestAsync())
                .AssertResult();
        }

        [Test]
        public async Task Crash()
        {
            var source = AsyncEnumerable.Range(1, 5)
                .Map(v => {
                    if (v == 4)
                    {
                        throw new InvalidOperationException();
                    }
                    return v.ToString();
                });

            (await source.TestAsync())
                .AssertFailure(typeof(InvalidOperationException), "1", "2", "3");
        }
    }
}
