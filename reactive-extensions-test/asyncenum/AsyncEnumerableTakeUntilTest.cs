using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Threading.Tasks;

namespace akarnokd.reactive_extensions_test.asyncenum
{
    [TestFixture]
    public class AsyncEnumerableTakeUntilTest
    {
        [Test]
        public async Task Basic()
        {
            var to = await AsyncEnumerable.Range(1, 5)
                .TakeUntil(AsyncEnumerable.Never<int>())
                .TestAsync();

            to.AssertResult(1, 2, 3, 4, 5);
        }

        [Test]
        public async Task Error_Main()
        {
            var to = await AsyncEnumerable.Error<int>(new InvalidOperationException())
                .TakeUntil(AsyncEnumerable.Never<int>())
                .TestAsync();

            to.AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public async Task Error_Other()
        {
            var to = await AsyncEnumerable.Never<int>()
                .TakeUntil(AsyncEnumerable.Error<int>(new InvalidOperationException()))
                .TestAsync();

            to.AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public async Task Other_Empty()
        {
            var to = await AsyncEnumerable.Never<int>()
                .TakeUntil(AsyncEnumerable.Empty<int>())
                .TestAsync();

            to.AssertResult();
        }

        [Test]
        public async Task Other_Just()
        {
            var to = await AsyncEnumerable.Never<int>()
                .TakeUntil(AsyncEnumerable.Just(1))
                .TestAsync();

            to.AssertResult();
        }

        [Test]
        public async Task Timed()
        {
            var to = await Task.Delay(100).ToAsyncEnumerable<int>()
                .TakeUntil(Task.Delay(100).ToAsyncEnumerable<int>())
                .TestAsync();

            to.AssertResult();
        }
    }
}
