using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.single
{
    [TestFixture]
    public class SingleNeverTest
    {
        [Test]
        public void Basic()
        {
            SingleSource.Never<int>()
                .Test()
                .AssertSubscribed()
                .AssertEmpty();
        }
    }
}
