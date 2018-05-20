using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.maybe
{
    [TestFixture]
    public class MaybeToSingleTest
    {
        [Test]
        public void Success()
        {
            ISingleSource<int> src = MaybeSource.Just(1)
                .ToSingle();

            src.Test().AssertResult(1);
        }

        [Test]
        public void Error()
        {
            ISingleSource<int> src = MaybeSource.Error<int>(new InvalidOperationException())
                .ToSingle();

            src.Test().AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Empty()
        {
            ISingleSource<int> src = MaybeSource.Empty<int>()
                .ToSingle();

            src.Test().AssertFailure(typeof(IndexOutOfRangeException));
        }

        [Test]
        public void Dispose()
        {
            TestHelper.VerifyDisposeMaybe<int, int>(m => m.ToSingle());
        }
    }
}
