using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.maybe
{
    [TestFixture]
    public class MaybeEmptyTest
    {
        [Test]
        public void Basic()
        {
            MaybeSource.Empty<int>()
                .Test()
                .AssertResult();
        }
    }
}
