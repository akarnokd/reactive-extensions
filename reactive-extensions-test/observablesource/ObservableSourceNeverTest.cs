using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.observablesource
{
    [TestFixture]
    public class ObservableSourceNeverTest
    {
        [Test]
        public void Basic()
        {
            ObservableSource.Never<int>()
                .Test()
                .AssertEmpty();
        }
    }
}
