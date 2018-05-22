using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.single
{
    [TestFixture]
    public class SingleToMaybeTest
    {
        [Test]
        public void Success()
        {
            IMaybeSource<int> src = SingleSource.Just(1)
                .ToMaybe();

            src.Test().AssertResult(1);
        }

        [Test]
        public void Error()
        {
            IMaybeSource<int> src = SingleSource.Error<int>(new InvalidOperationException())
                .ToMaybe();

            src.Test().AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Dispose()
        {
            TestHelper.VerifyDisposeSingle<int, int>(m => m.ToMaybe());
        }
    }
}
