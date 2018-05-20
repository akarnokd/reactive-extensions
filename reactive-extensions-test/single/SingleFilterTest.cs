using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.single
{
    [TestFixture]
    public class SingleFilterTest
    {
        [Test]
        public void Basic()
        {
            SingleSource.Just(1)
                .Filter(v => true)
                .Test()
                .AssertResult(1);
        }

        [Test]
        public void Basic_Filtered()
        {
            SingleSource.Just(1)
                .Filter(v => false)
                .Test()
                .AssertResult();
        }

        [Test]
        public void Error()
        {
            SingleSource.Error<int>(new InvalidOperationException())
                .Filter(v => true)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Predicate_Crash()
        {
            SingleSource.Just(1)
                .Filter(v =>
                {
                    throw new InvalidOperationException();
                })
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Dispose()
        {
            TestHelper.VerifyDisposeSingle<int, int>(m => m.Filter(v => true));
        }
    }
}
