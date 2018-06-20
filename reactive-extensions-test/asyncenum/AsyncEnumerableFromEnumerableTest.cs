using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Threading.Tasks;
using System.Linq;

namespace akarnokd.reactive_extensions_test.asyncenum
{
    [TestFixture]
    public class AsyncEnumerableFromEnumerableTest
    {
        [Test]
        public async Task Basic()
        {
            var to = await AsyncEnumerable.FromEnumerable(Enumerable.Range(1, 5)).TestAsync();

            to.AssertResult(1, 2, 3, 4, 5);
        }

        [Test]
        public async Task Basic_Fused()
        {
            var to = await Enumerable.Range(1, 5).ToAsyncEnumerable().TestAsyncFused();

            to.AssertResult(1, 2, 3, 4, 5);
        }

        [Test]
        public async Task Empty()
        {
            var to = await AsyncEnumerable.FromEnumerable(Enumerable.Empty<int>()).TestAsync();

            to.AssertResult();
        }

        [Test]
        public async Task Empty_Fused()
        {
            var to = await AsyncEnumerable.FromEnumerable(Enumerable.Empty<int>()).TestAsyncFused();

            to.AssertResult();
        }

        [Test]
        public async Task GetEnumerator_Crash()
        {
            var to = await AsyncEnumerable.FromEnumerable(new FailingEnumerable<int>(true, false, false)).TestAsync();

            to.AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public async Task MoveNext_Crash()
        {
            var to = await AsyncEnumerable.FromEnumerable(new FailingEnumerable<int>(false, true, false)).TestAsync();

            to.AssertFailure(typeof(InvalidOperationException));
        }
    }
}
