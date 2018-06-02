using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.observablesource
{
    [TestFixture]
    public class ObservableSourceComposeTest
    {
        [Test]
        public void Basic()
        {
            ObservableSource.Just(1)
                .Compose(v => v)
                .Test()
                .AssertResult(1);
        }
    }
}
