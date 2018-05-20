using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.maybe
{
    [TestFixture]
    public class MaybeDefaultIfEmptyTest
    {
        [Test]
        public void Success()
        {
            MaybeSource.Just(1)
                .DefaultIfEmpty(2)
                .Test()
                .AssertResult(1);
        }

        [Test]
        public void Empty()
        {
            MaybeSource.Empty<int>()
                .DefaultIfEmpty(2)
                .Test()
                .AssertResult(2);
        }

        [Test]
        public void Error()
        {
            MaybeSource.Error<int>(new InvalidOperationException())
                .DefaultIfEmpty(2)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Dispose()
        {
            TestHelper.VerifyDisposeMaybe<int, int>(m => m.DefaultIfEmpty(1));
        }
    }
}
