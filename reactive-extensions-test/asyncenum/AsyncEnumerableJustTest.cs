using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Threading.Tasks;

namespace akarnokd.reactive_extensions_test.asyncenum
{
    [TestFixture]
    public class AsyncEnumerableJustTest
    {
        [Test]
        public async Task Basic()
        {
            var source = AsyncEnumerable.Just(1);

            for (int i = 0; i < 10; i++)
            {
                var en = source.GetAsyncEnumerator();
                try
                {
                    Assert.True(await en.MoveNextAsync());
                    Assert.AreEqual(1, en.Current);

                    Assert.False(await en.MoveNextAsync());
                }
                finally
                {
                    await en.DisposeAsync();
                }
            }
        }

        [Test]
        public async Task Fused()
        {
            var source = AsyncEnumerable.Just(1);

            for (int i = 0; i < 10; i++)
            {
                var en = source.GetAsyncEnumerator();
                try
                {
                    var f = en as IAsyncFusedEnumerator<int>;

                    Assert.NotNull(f);

                    var v = f.TryPoll(out var state);

                    Assert.AreEqual(AsyncFusedState.Ready, state);
                    Assert.AreEqual(1, v);

                    v = f.TryPoll(out state);

                    Assert.AreEqual(AsyncFusedState.Terminated, state);
                    Assert.AreEqual(default(int), v);
                }
                finally
                {
                    await en.DisposeAsync();
                }
            }
        }
    }
}
