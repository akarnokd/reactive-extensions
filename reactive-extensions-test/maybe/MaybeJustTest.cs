using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.maybe
{
    [TestFixture]
    public class MaybeJustTest
    {
        [Test]
        public void Basic()
        {
            MaybeSource.Just(1)
                .Test()
                .AssertResult(1);
        }
    }
}
