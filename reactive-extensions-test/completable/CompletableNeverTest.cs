using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.completable
{
    [TestFixture]
    public class CompletableNeverTest
    {
        [Test]
        public void Basic()
        {
            CompletableSource.Never()
                .Test()
                .AssertSubscribed()
                .AssertEmpty();
        }
    }
}
