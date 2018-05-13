using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.completable
{
    [TestFixture]
    public class CompletableEmptyTest
    {
        [Test]
        public void Basic()
        {
            CompletableSource.Empty()
                .Test()
                .AssertResult();
        }
    }
}
