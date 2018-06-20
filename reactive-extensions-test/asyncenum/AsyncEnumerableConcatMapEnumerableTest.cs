using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Threading.Tasks;
using System.Linq;

namespace akarnokd.reactive_extensions_test.asyncenum
{
    [TestFixture]
    public class AsyncEnumerableConcatMapEnumerableTest
    {
        [Test]
        public async Task Basic()
        {
            var to = await AsyncEnumerable.Range(1, 5)
                .ConcatMap(v => Enumerable.Range(v * 100, 5))
                .TestAsync();

            to.AssertResult(
                100, 101, 102, 103, 104,
                200, 201, 202, 203, 204,
                300, 301, 302, 303, 304,
                400, 401, 402, 403, 404,
                500, 501, 502, 503, 504
            );
        }

        [Test]
        public async Task Empty_Outer()
        {
            var to = await AsyncEnumerable.Empty<int>()
                .ConcatMap(v => Enumerable.Range(v * 100, 5))
                .TestAsync();

            to.AssertResult();
        }

        [Test]
        public async Task Empty_Inner()
        {
            var to = await AsyncEnumerable.Range(1, 5)
                .ConcatMap(v => Enumerable.Empty<int>())
                .TestAsync();

            to.AssertResult();
        }
    }
}
