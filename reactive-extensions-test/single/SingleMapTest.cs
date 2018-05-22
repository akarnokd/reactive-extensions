using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.single
{
    [TestFixture]
    public class SingleMapTest
    {
        [Test]
        public void Basic()
        {
            SingleSource.Just(1)
                .Map(v => "" + (v + 1))
                .Test()
                .AssertResult("2");
        }

        [Test]
        public void Error()
        {
            SingleSource.Error<int>(new InvalidOperationException())
                .Map(v => "" + (v + 1))
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Mapper_Crash()
        {
            SingleSource.Just(1)
                .Map<int, string>(v => { throw new InvalidOperationException(); })
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Dispose()
        {
            TestHelper.VerifyDisposeSingle<int, int>(m => m.Map(v => v));
        }
    }
}
