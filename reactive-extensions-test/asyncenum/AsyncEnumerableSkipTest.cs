using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Threading.Tasks;

namespace akarnokd.reactive_extensions_test.asyncenum
{
    [TestFixture]
    public class AsyncEnumerableSkipTest
    {
        [Test]
        public async Task Basic()
        {
            var to = await AsyncEnumerable.Range(1, 10)
                .Skip(5)
                .TestAsync();
                
            to.AssertResult(6, 7, 8, 9, 10);
        }

        [Test]
        public async Task Empty()
        {
            var to = await AsyncEnumerable.Empty<int>()
                .Skip(5)
                .TestAsync();

            to.AssertResult();
        }

        [Test]
        public async Task Zero()
        {
            var to = await AsyncEnumerable.Range(1, 10)
                .Skip(0)
                .TestAsync();

            to.AssertResult(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
        }

        [Test]
        public async Task All_Exact()
        {
            var to = await AsyncEnumerable.Range(1, 10)
                .Skip(10)
                .TestAsync();

            to.AssertResult();
        }

        [Test]
        public async Task All_More()
        {
            var to = await AsyncEnumerable.Range(1, 10)
                .Skip(15)
                .TestAsync();

            to.AssertResult();
        }
    }
}
