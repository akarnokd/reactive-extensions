using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.maybe
{
    [TestFixture]
    public class MaybeOnErrorCompleteTest
    {
        [Test]
        public void Success()
        {
            MaybeSource.Just(1)
                .OnErrorComplete()
                .Test()
                .AssertResult(1);
        }

        [Test]
        public void Empty()
        {
            MaybeSource.Empty<int>()
                .OnErrorComplete()
                .Test()
                .AssertResult();
        }

        [Test]
        public void Error()
        {
            MaybeSource.Error<int>(new InvalidOperationException())
                .OnErrorComplete()
                .Test()
                .AssertResult();
        }

        [Test]
        public void Dispose()
        {
            TestHelper.VerifyDisposeMaybe<int, int>(m => m.OnErrorComplete());
        }
    }
}
