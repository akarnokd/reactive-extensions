using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.completable
{
    [TestFixture]
    public class CompletableDeferTest
    {
        [Test]
        public void Basic()
        {
            var count = 0;

            var c = CompletableSource.Defer(() =>
            {
                count++;
                return CompletableSource.Empty();
            });

            for (int i = 0; i < 100; i++)
            {
                Assert.AreEqual(i, count);

                c.Test().AssertResult();

                Assert.AreEqual(i + 1, count);
            }
        }

        [Test]
        public void Supplier_Crash()
        {
            var c = CompletableSource.Defer(() =>
            {
                throw new InvalidOperationException();
            });

            c.Test().AssertFailure(typeof(InvalidOperationException));
        }
    }
}
