using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Threading.Tasks;

namespace akarnokd.reactive_extensions_test.asyncenum
{
    [TestFixture]
    public class AsyncEnumerableRangeTest
    {
        [Test]
        public async Task Basic()
        {
            var to = await AsyncEnumerable.Range(1, 5).TestAsync();

            to.AssertResult(1, 2, 3, 4, 5);
        }

        [Test]
        public async Task Empty()
        {
            var to = await AsyncEnumerable.Range(1, 0).TestAsync();

            to.AssertResult();
        }

        [Test]
        public async Task Fused()
        {
            var to = await AsyncEnumerable.Range(1, 5).TestAsyncFused();

            to.AssertResult(1, 2, 3, 4, 5);
        }

        [Test]
        public async Task Fused_Empty()
        {
            var to = await AsyncEnumerable.Range(1, 0).TestAsyncFused();

            to.AssertResult();
        }

        [Test]
        public async Task Basic_Long()
        {
            var to = await AsyncEnumerable.Range(1, 1000).TestAsync();

            to.AssertValueCount(1000)
                .AssertNoError()
                .AssertCompleted();
        }

        [Test]
        public async Task Fused_Long()
        {
            var to = await AsyncEnumerable.Range(1, 1000).TestAsyncFused();

            to.AssertValueCount(1000)
                .AssertNoError()
                .AssertCompleted();
        }
    }
}
