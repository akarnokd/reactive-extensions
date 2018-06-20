using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Threading.Tasks;

namespace akarnokd.reactive_extensions_test.asyncenum
{
    [TestFixture]
    public class AsyncEnumerableFromTaskTest
    {
        [Test]
        public async Task Plain_Basic()
        {
            var to = await AsyncEnumerable.FromTask<int>(Task.Delay(100)).TestAsync();

            to.AssertResult();
        }

        [Test]
        public async Task Plain_Basic_2()
        {
            var to = await Task.Delay(100).ToAsyncEnumerable<int>().TestAsync();

            to.AssertResult();
        }

        [Test]
        public async Task Plain_Error()
        {
            var to = await AsyncEnumerable.FromTask<int>(Task.FromException(new InvalidOperationException()))
                .TestAsync();

            to.AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public async Task Typed_Basic()
        {
            var to = await AsyncEnumerable.FromTask(Task.Delay(100).ContinueWith(t => 1)).TestAsync();

            to.AssertResult(1);
        }

        [Test]
        public async Task Typed_Basic_2()
        {
            var to = await Task.Delay(100).ContinueWith(t => 1).ToAsyncEnumerable().TestAsync();

            to.AssertResult(1);
        }

        [Test]
        public async Task Typed_Error()
        {
            var to = await AsyncEnumerable.FromTask<int>(
                    Task.Delay(100).ContinueWith<int>(t => throw new InvalidOperationException())
                )
                .TestAsync();

            to.AssertFailure(typeof(InvalidOperationException));
        }
    }
}
