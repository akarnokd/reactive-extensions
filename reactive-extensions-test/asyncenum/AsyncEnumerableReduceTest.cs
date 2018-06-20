using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Threading.Tasks;

namespace akarnokd.reactive_extensions_test.asyncenum
{
    [TestFixture]
    public class AsyncEnumerableReduceTest
    {
        [Test]
        public async Task Plain_Basic()
        {
            var to = await AsyncEnumerable.Range(1, 5).Reduce((a, b) => a + b).TestAsync();

            to.AssertResult(15);
        }

        [Test]
        public async Task Plain_Empty()
        {
            var to = await AsyncEnumerable.Empty<int>().Reduce((a, b) => a + b).TestAsync();

            to.AssertResult();
        }

        [Test]
        public async Task Plain_Reducer_Crash()
        {
            var to = await AsyncEnumerable.Range(1, 5).Reduce((a, b) => throw new InvalidOperationException())
                .TestAsync();

            to.AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public async Task Supplier_Basic()
        {
            var to = await AsyncEnumerable.Range(1, 5).Reduce(() => 10, (a, b) => a + b).TestAsync();

            to.AssertResult(25);
        }

        [Test]
        public async Task Supplier_Empty()
        {
            var to = await AsyncEnumerable.Empty<int>().Reduce(() => 10, (a, b) => a + b).TestAsync();

            to.AssertResult(10);
        }

        [Test]
        public async Task Supplier_Crash()
        {
            var to = await AsyncEnumerable.Empty<int>().Reduce<int, int>(
                    () => throw new InvalidOperationException(),
                    (a, b) => a + b
                )
                .TestAsync();

            to.AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public async Task Supplier_Reducer_Crash()
        {
            var to = await AsyncEnumerable.Range(1, 5).Reduce(() => 10, (a, b) => throw new InvalidOperationException())
                .TestAsync();

            to.AssertFailure(typeof(InvalidOperationException));
        }
    }
}
