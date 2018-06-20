using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace akarnokd.reactive_extensions_test.asyncenum
{
    [TestFixture]
    public class AsyncEnumerableCollectTest
    {
        [Test]
        public async Task Basic()
        {
            var to = await AsyncEnumerable.Range(1, 5)
                .Collect(() => new List<int>(), (a, b) => a.Add(b))
                .TestAsync();

            to.AssertResult(new List<int>() { 1, 2, 3, 4, 5 });
        }

        [Test]
        public async Task Empty()
        {
            var to = await AsyncEnumerable.Empty<int>()
                .Collect(() => new List<int>(), (a, b) => a.Add(b))
                .TestAsync();

            to.AssertResult(new List<int>() { });
        }

        [Test]
        public async Task Supplier_Crash()
        {
            var to = await AsyncEnumerable.Range(1, 5)
                .Collect<int, List<int>>(() => throw new InvalidOperationException(), (a, b) => a.Add(b))
                .TestAsync();

            to.AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public async Task Collector_Crash()
        {
            var to = await AsyncEnumerable.Range(1, 5)
                .Collect(() => new List<int>(), (a, b) => throw new InvalidOperationException())
                .TestAsync();

            to.AssertFailure(typeof(InvalidOperationException));
        }
    }
}
