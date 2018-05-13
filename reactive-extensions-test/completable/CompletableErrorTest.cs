using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.completable
{
    [TestFixture]
    public class CompletableErrorTest
    {
        [Test]
        public void Basic()
        {
            CompletableSource.Error(new InvalidOperationException())
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }
    }
}
