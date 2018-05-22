using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.single
{
    [TestFixture]
    public class SingleOnErrorResumeNextTest
    {
        #region + Fallback +

        [Test]
        public void Success()
        {
            var count = 0;
            var fb = SingleSource.FromFunc<int>(() => count++);

            SingleSource.Just(1)
                .OnErrorResumeNext(fb)
                .Test()
                .AssertResult(1);

            Assert.AreEqual(0, count);
        }

        [Test]
        public void Error_Fallback_Empty()
        {
            var count = 0;
            var fb = SingleSource.FromFunc<int>(() => count++);

            SingleSource.Error<int>(new InvalidOperationException())
                .OnErrorResumeNext(fb)
                .Test()
                .AssertResult(0);

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Error_Fallback_Success()
        {
            var count = 0;
            var fb = SingleSource.FromFunc(() => ++count);

            SingleSource.Error<int>(new InvalidOperationException())
                .OnErrorResumeNext(fb)
                .Test()
                .AssertResult(1);

            Assert.AreEqual(1, count);
        }
        [Test]
        public void Fallback_Error()
        {
            SingleSource.Error<int>(new InvalidOperationException("main"))
                .OnErrorResumeNext(SingleSource.Error<int>(new InvalidOperationException("fallback")))
                .Test()
                .AssertFailure(typeof(InvalidOperationException))
                .AssertError(typeof(InvalidOperationException), "fallback");
        }

        [Test]
        public void Dispose_Main()
        {
            TestHelper.VerifyDisposeSingle<int, int>(m => m.OnErrorResumeNext(SingleSource.Just<int>(1)));
        }

        [Test]
        public void Dispose_Fallback()
        {
            TestHelper.VerifyDisposeSingle<int, int>(m => 
                SingleSource.Error<int>(new InvalidOperationException())
                .OnErrorResumeNext(m)
            );
        }

        #endregion + Fallback +

        #region + Handler +

        [Test]
        public void Handler_Success()
        {
            var count = 0;
            var fb = SingleSource.FromFunc<int>(() => count++);

            SingleSource.Just(1)
                .OnErrorResumeNext(e => fb)
                .Test()
                .AssertResult(1);

            Assert.AreEqual(0, count);
        }

        [Test]
        public void Handler_Error()
        {
            var count = 0;
            var fb = SingleSource.FromFunc<int>(() => count++);

            SingleSource.Error<int>(new InvalidOperationException())
                .OnErrorResumeNext(e => fb)
                .Test()
                .AssertResult(0);

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Handler_Error_Success()
        {
            var count = 0;
            var fb = SingleSource.FromFunc(() => ++count);

            SingleSource.Error<int>(new InvalidOperationException())
                .OnErrorResumeNext(e => fb)
                .Test()
                .AssertResult(1);

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Handler_Dispose_Main()
        {
            TestHelper.VerifyDisposeSingle<int, int>(m =>
                m.OnErrorResumeNext(e => SingleSource.Just(1))
            );
        }

        [Test]
        public void Handler_Dispose_Fallback()
        {
            TestHelper.VerifyDisposeSingle<int, int>(m =>
                SingleSource.Error<int>(new InvalidOperationException())
                .OnErrorResumeNext(e => m)
            );
        }


        [Test]
        public void Handler_Fallback_Error()
        {
            SingleSource.Error<int>(new InvalidOperationException("main"))
                .OnErrorResumeNext(e => SingleSource.Error<int>(new InvalidOperationException("fallback")))
                .Test()
                .AssertFailure(typeof(InvalidOperationException))
                .AssertError(typeof(InvalidOperationException), "fallback");
        }

        [Test]
        public void Handler_Crash()
        {
            SingleSource.Error<int>(new InvalidOperationException("main"))
                .OnErrorResumeNext(e => { throw new InvalidOperationException("fallback"); })
                .Test()
                .AssertFailure(typeof(AggregateException))
                .AssertCompositeErrorCount(2)
                .AssertCompositeError(0, typeof(InvalidOperationException), "main")
                .AssertCompositeError(1, typeof(InvalidOperationException), "fallback");
        }

        #endregion + Handler +
    }
}
