using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Threading.Tasks;

namespace akarnokd.reactive_extensions_test.asyncenum
{
    [TestFixture]
    public class AsyncEnumerableEmptyTest
    {
        [Test]
        public void Singleton()
        {
            Assert.AreSame(AsyncEnumerable.Empty<int>(), AsyncEnumerable.Empty<int>());
        }

        [Test]
        public async Task Basic()
        {
            var en = AsyncEnumerable.Empty<int>().GetAsyncEnumerator();

            try
            {
                Assert.False(await en.MoveNextAsync(), "#1: Should have been false");

                Assert.False(await en.MoveNextAsync(), "#2: Should have been false");

            }
            finally
            {
                await en.DisposeAsync();
            }
        }

        [Test]
        public async Task Fused()
        {
            var en = AsyncEnumerable.Empty<int>().GetAsyncEnumerator();

            try
            {
                var f = en as IAsyncFusedEnumerator<int>;

                Assert.NotNull(f);

                var v = f.TryPoll(out var state);

                Assert.AreEqual(default(int), v);
                Assert.AreEqual(AsyncFusedState.Terminated, state);
            }
            finally
            {
                await en.DisposeAsync();
            }
        }
    }
}
