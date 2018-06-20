using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Threading.Tasks;

namespace akarnokd.reactive_extensions_test.asyncenum
{
    [TestFixture]
    public class AsyncEnumerableFilterTest
    {
        [Test]
        public async Task Basic()
        {
            var to = await AsyncEnumerable.Range(1, 10)
                .Filter(v => v % 2 == 0)
                .TestAsync();
                
            to.AssertResult(2, 4, 6, 8, 10);
        }

        [Test]
        public async Task Empty()
        {
            var to = await AsyncEnumerable.Empty<int>()
                .Filter(v => v % 2 == 0)
                .TestAsync();

            to.AssertResult();
        }

        [Test]
        public async Task Crash()
        {
            var to = await AsyncEnumerable.Range(1, 10)
                .Filter(v =>
                {
                    if (v == 7)
                    {
                        throw new InvalidOperationException();
                    }
                    return v % 2 == 0;
                })
                .TestAsync();

            to.AssertFailure(typeof(InvalidOperationException), 2, 4, 6);
        }
    }
}
