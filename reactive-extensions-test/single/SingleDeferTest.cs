using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.single
{
    [TestFixture]
    public class SingleDeferTest
    {
        [Test]
        public void Success()
        {
            var count = 0;

            var c = SingleSource.Defer(() =>
            {
                count++;
                return SingleSource.Just(count);
            });

            for (int i = 0; i < 100; i++)
            {
                Assert.AreEqual(i, count);

                c.Test().AssertResult(i + 1);

                Assert.AreEqual(i + 1, count);
            }
        }

        [Test]
        public void Supplier_Crash()
        {
            var c = SingleSource.Defer<int>(() =>
            {
                throw new InvalidOperationException();
            });

            c.Test().AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Dispose()
        {
            TestHelper.VerifyDisposeSingle<int, int>(m => SingleSource.Defer(() => m));
        }
    }
}
