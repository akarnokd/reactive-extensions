using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Threading.Tasks;

namespace akarnokd.reactive_extensions_test.asyncenum
{
    [TestFixture]
    public class AsyncEnumerableErrorTest
    {
        [Test]
        public async Task Basic()
        {
            var to = await AsyncEnumerable.Error<int>(new InvalidOperationException()).TestAsync();

            to.AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public async Task Fused()
        {
            var to = await AsyncEnumerable.Error<int>(new InvalidOperationException()).TestAsyncFused();

            to.AssertFailure(typeof(InvalidOperationException));
        }
    }
}
