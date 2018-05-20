using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.maybe
{
    [TestFixture]
    public class MaybeDeferTest
    {
        [Test]
        public void Basic()
        {
            var count = 0;

            var c = MaybeSource.Defer(() =>
            {
                count++;
                return MaybeSource.Empty<int>();
            });

            for (int i = 0; i < 100; i++)
            {
                Assert.AreEqual(i, count);

                c.Test().AssertResult();

                Assert.AreEqual(i + 1, count);
            }
        }

        [Test]
        public void Success()
        {
            var count = 0;

            var c = MaybeSource.Defer(() =>
            {
                count++;
                return MaybeSource.Just(count);
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
            var c = MaybeSource.Defer<int>(() =>
            {
                throw new InvalidOperationException();
            });

            c.Test().AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Dispose()
        {
            TestHelper.VerifyDisposeMaybe<int, int>(m => MaybeSource.Defer(() => m));
        }
    }
}
