using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Threading.Tasks;

namespace akarnokd.reactive_extensions_test.asyncenum
{
    [TestFixture]
    public class AsyncEnumerableConcatArrayTest
    {
        [Test]
        public async Task Basic()
        {
            var to = await AsyncEnumerable.Concat(AsyncEnumerable.Range(1, 5), AsyncEnumerable.Range(6, 5))
                .TestAsync();

            to.AssertResult(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
        }

        [Test]
        public async Task Take_4()
        {
            var to = await AsyncEnumerable.Concat(AsyncEnumerable.Range(1, 5), AsyncEnumerable.Range(6, 5))
                .Take(4)
                .TestAsync();

            to.AssertResult(1, 2, 3, 4);
        }

        [Test]
        public async Task Take_7()
        {
            var to = await AsyncEnumerable.Concat(AsyncEnumerable.Range(1, 5), AsyncEnumerable.Range(6, 5))
                .Take(7)
                .TestAsync();

            to.AssertResult(1, 2, 3, 4, 5, 6, 7);
        }

        [Test]
        public async Task Empty()
        {
            var to = await AsyncEnumerable.Concat<int>()
                .TestAsync();

            to.AssertResult();
        }

        [Test]
        public async Task Empty_Sources()
        {
            var to = await AsyncEnumerable.Concat(AsyncEnumerable.Empty<int>(), AsyncEnumerable.Empty<int>(), AsyncEnumerable.Empty<int>())
                .TestAsync();

            to.AssertResult();
        }

        [Test]
        public async Task Error()
        {
            var to = await AsyncEnumerable.Concat(AsyncEnumerable.Range(1, 3), AsyncEnumerable.Error<int>(new InvalidOperationException()))
                .TestAsync();

            to.AssertFailure(typeof(InvalidOperationException), 1, 2, 3);
        }
    }
}
