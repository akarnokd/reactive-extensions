using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.completable
{
    [TestFixture]
    public class CompletableIgnoreElementTest
    {
        [Test]
        public void Single_Success()
        {
            SingleSource.Just(1)
                .IgnoreElement()
                .Test()
                .AssertResult();
        }

        [Test]
        public void Single_Error()
        {
            SingleSource.Error<int>(new InvalidOperationException())
                .IgnoreElement()
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Single_Dispose()
        {
            var ss = new SingleSubject<int>();

            var to = ss.IgnoreElement().Test();

            Assert.True(ss.HasObserver());

            to.Dispose();

            Assert.False(ss.HasObserver());
        }

        [Test]
        public void Maybe_Success()
        {
            MaybeSource.Just(1)
                .IgnoreElement()
                .Test()
                .AssertResult();
        }

        [Test]
        public void Maybe_Completed()
        {
            MaybeSource.Empty<int>()
                .IgnoreElement()
                .Test()
                .AssertResult();
        }

        [Test]
        public void Maybe_Error()
        {
            MaybeSource.Error<int>(new InvalidOperationException())
                .IgnoreElement()
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Maybe_Dispose()
        {
            var ss = new MaybeSubject<int>();

            var to = ss.IgnoreElement().Test();

            Assert.True(ss.HasObserver());

            to.Dispose();

            Assert.False(ss.HasObserver());
        }
    }
}
