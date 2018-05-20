using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.maybe
{
    [TestFixture]
    public class MaybeMapTest
    {
        [Test]
        public void Basic()
        {
            MaybeSource.Just(1)
                .Map(v => "" + (v + 1))
                .Test()
                .AssertResult("2");
        }

        [Test]
        public void Empty()
        {
            MaybeSource.Empty<int>()
                .Map(v => "" + (v + 1))
                .Test()
                .AssertResult();
        }

        [Test]
        public void Error()
        {
            MaybeSource.Error<int>(new InvalidOperationException())
                .Map(v => "" + (v + 1))
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Mapper_Crash()
        {
            MaybeSource.Just(1)
                .Map<int, string>(v => { throw new InvalidOperationException(); })
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Dispose()
        {
            TestHelper.VerifyDisposeMaybe<int, int>(m => m.Map(v => v));
        }
    }
}
