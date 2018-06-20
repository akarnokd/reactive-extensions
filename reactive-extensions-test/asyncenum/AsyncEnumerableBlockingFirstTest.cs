using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Threading.Tasks;

namespace akarnokd.reactive_extensions_test.asyncenum
{
    [TestFixture]
    public class AsyncEnumerableBlockingFirstTest
    {
        [Test]
        public void Basic_Range()
        {
            Assert.AreEqual(1, AsyncEnumerable.Range(1, 5).BlockingFirst());
        }

        [Test]
        public void Basic_Just()
        {
            Assert.AreEqual(1, AsyncEnumerable.Just(1).BlockingFirst());
        }

        [Test]
        public void Basic_Empty()
        {
            try
            {
                AsyncEnumerable.Empty<int>().BlockingFirst();
                Assert.Fail("Should have thrown");
            }
            catch (IndexOutOfRangeException)
            {
                // expected
            }
        }

        [Test]
        public void Basic_Error()
        {
            try
            {
                AsyncEnumerable.Error<int>(new InvalidOperationException()).BlockingFirst();
                Assert.Fail("Should have thrown");
            }
            catch (InvalidOperationException)
            {
                // expected
            }
        }
    }
}
