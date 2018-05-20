using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.single
{
    [TestFixture]
    public class SingleJustTest
    {
        [Test]
        public void Basic()
        {
            SingleSource.Just(1)
                .Test()
                .AssertResult(1);
        }
    }
}
