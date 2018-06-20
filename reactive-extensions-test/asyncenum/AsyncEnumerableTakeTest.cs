using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Threading.Tasks;

namespace akarnokd.reactive_extensions_test.asyncenum
{
    [TestFixture]
    public class AsyncEnumerableTakeTest
    {
        [Test]
        public async Task Basic()
        {
            var to = await AsyncEnumerable.Range(1, 10)
                .Take(5)
                .TestAsync();
                
            to.AssertResult(1, 2, 3, 4, 5);
        }

        [Test]
        public async Task Empty()
        {
            var to = await AsyncEnumerable.Empty<int>()
                .Take(5)
                .TestAsync();

            to.AssertResult();
        }

        [Test]
        public async Task Zero()
        {
            var to = await AsyncEnumerable.Range(1, 10)
                .Take(0)
                .TestAsync();

            to.AssertResult();
        }

    }
}
