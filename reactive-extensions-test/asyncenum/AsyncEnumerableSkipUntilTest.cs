using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Threading.Tasks;

namespace akarnokd.reactive_extensions_test.asyncenum
{
    [TestFixture]
    public class AsyncEnumerableSkipUntilTest
    {
        [Test]
        public async Task Basic()
        {
            var to = await AsyncEnumerable.Range(1, 5)
                    .SkipUntil(AsyncEnumerable.Just(1))
                    .TestAsync();

            to.AssertResult(1, 2, 3, 4, 5);
        }

        [Test]
        public async Task Skip_All()
        {
            var to = await AsyncEnumerable.Range(1, 100000)
                    .SkipUntil(AsyncEnumerable.Never<int>())
                    .TestAsync();

            to.AssertResult();
        }

        [Test]
        public async Task Basic_Timed()
        {
            var to = await AsyncEnumerable.Concat(
                    Task.Delay(50).ContinueWith(v => 1).ToAsyncEnumerable(),
                    Task.Delay(150).ContinueWith(v => 2).ToAsyncEnumerable()
                    )
                    .SkipUntil(Task.Delay(100).ToAsyncEnumerable<int>())
                    .TestAsync();

            to.AssertResult(2);
        }

        [Test]
        public async Task Empty()
        {
            var to = await AsyncEnumerable.Empty<int>()
                .SkipUntil(AsyncEnumerable.Never<int>())
                .TestAsync();

            to.AssertResult();
        }

        [Test]
        public async Task Error_Main()
        {
            var to = await AsyncEnumerable.Error<int>(new InvalidOperationException())
                .SkipUntil(AsyncEnumerable.Never<int>())
                .TestAsync();

            to.AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public async Task Error_Other()
        {
            var to = await AsyncEnumerable.Never<int>()
                .SkipUntil(AsyncEnumerable.Error<int>(new InvalidOperationException()))
                .TestAsync();

            to.AssertFailure(typeof(InvalidOperationException));
        }
    }
}
