using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.single
{
    [TestFixture]
    public class SingleErrorTest
    {
        [Test]
        public void Basic()
        {
            SingleSource.Error<int>(new InvalidOperationException())
                .Test()
                .AssertSubscribed()
                .AssertFailure(typeof(InvalidOperationException));
        }
    }
}
