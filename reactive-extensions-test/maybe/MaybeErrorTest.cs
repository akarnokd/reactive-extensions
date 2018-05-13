using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.maybe
{
    [TestFixture]
    public class MaybeErrorTest
    {
        [Test]
        public void Basic()
        {
            MaybeSource.Error<int>(new InvalidOperationException())
                .Test()
                .AssertSubscribed()
                .AssertFailure(typeof(InvalidOperationException));
        }
    }
}
