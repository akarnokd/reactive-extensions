using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Threading.Tasks;

namespace akarnokd.reactive_extensions_test.asyncenum
{
    [TestFixture]
    public class AsyncEnumerableDeferTest
    {
        [Test]
        public async Task Basic()
        {
            var count = 0;

            var source = AsyncEnumerable.Defer(() => AsyncEnumerable.Just(++count));

            for (int i = 1; i < 11; i++)
            {
                (await source.TestAsync()).AssertResult(i);
            }
        }

        [Test]
        public async Task Crash()
        {
            var source = AsyncEnumerable.Defer<int>(() => throw new InvalidOperationException());

            (await source.TestAsync()).AssertFailure(typeof(InvalidOperationException));
        }
    }
}
