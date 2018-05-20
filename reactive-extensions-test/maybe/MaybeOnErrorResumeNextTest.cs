using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.maybe
{
    [TestFixture]
    public class MaybeOnErrorResumeNextTest
    {
        #region + Fallback +

        [Test]
        public void Success()
        {
            var count = 0;
            var fb = MaybeSource.FromAction<int>(() => count++);

            MaybeSource.Just(1)
                .OnErrorResumeNext(fb)
                .Test()
                .AssertResult(1);

            Assert.AreEqual(0, count);
        }

        [Test]
        public void Empty()
        {
            var count = 0;
            var fb = MaybeSource.FromAction<int>(() => count++);

            MaybeSource.Empty<int>()
                .OnErrorResumeNext(fb)
                .Test()
                .AssertResult();

            Assert.AreEqual(0, count);
        }

        [Test]
        public void Error_Fallback_Empty()
        {
            var count = 0;
            var fb = MaybeSource.FromAction<int>(() => count++);

            MaybeSource.Error<int>(new InvalidOperationException())
                .OnErrorResumeNext(fb)
                .Test()
                .AssertResult();

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Error_Fallback_Success()
        {
            var count = 0;
            var fb = MaybeSource.FromFunc(() => ++count);

            MaybeSource.Error<int>(new InvalidOperationException())
                .OnErrorResumeNext(fb)
                .Test()
                .AssertResult(1);

            Assert.AreEqual(1, count);
        }
        [Test]
        public void Fallback_Error()
        {
            MaybeSource.Error<int>(new InvalidOperationException("main"))
                .OnErrorResumeNext(MaybeSource.Error<int>(new InvalidOperationException("fallback")))
                .Test()
                .AssertFailure(typeof(InvalidOperationException))
                .AssertError(typeof(InvalidOperationException), "fallback");
        }

        [Test]
        public void Dispose_Main()
        {
            TestHelper.VerifyDisposeMaybe<int, int>(m => m.OnErrorResumeNext(MaybeSource.Empty<int>()));
        }

        [Test]
        public void Dispose_Fallback()
        {
            TestHelper.VerifyDisposeMaybe<int, int>(m => 
                MaybeSource.Error<int>(new InvalidOperationException())
                .OnErrorResumeNext(m)
            );
        }

        #endregion + Fallback +

        #region + Handler +

        [Test]
        public void Handler_Success()
        {
            var count = 0;
            var fb = MaybeSource.FromAction<int>(() => count++);

            MaybeSource.Just(1)
                .OnErrorResumeNext(e => fb)
                .Test()
                .AssertResult(1);

            Assert.AreEqual(0, count);
        }

        [Test]
        public void Handler_Empty()
        {
            var count = 0;
            var fb = MaybeSource.FromAction<int>(() => count++);

            MaybeSource.Empty<int>()
                .OnErrorResumeNext(e => fb)
                .Test()
                .AssertResult();

            Assert.AreEqual(0, count);
        }

        [Test]
        public void Handler_Error()
        {
            var count = 0;
            var fb = MaybeSource.FromAction<int>(() => count++);

            MaybeSource.Error<int>(new InvalidOperationException())
                .OnErrorResumeNext(e => fb)
                .Test()
                .AssertResult();

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Handler_Error_Success()
        {
            var count = 0;
            var fb = MaybeSource.FromFunc(() => ++count);

            MaybeSource.Error<int>(new InvalidOperationException())
                .OnErrorResumeNext(e => fb)
                .Test()
                .AssertResult(1);

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Handler_Dispose_Main()
        {
            TestHelper.VerifyDisposeMaybe<int, int>(m =>
                m.OnErrorResumeNext(e => MaybeSource.Empty<int>())
            );
        }

        [Test]
        public void Handler_Dispose_Fallback()
        {
            TestHelper.VerifyDisposeMaybe<int, int>(m =>
                MaybeSource.Error<int>(new InvalidOperationException())
                .OnErrorResumeNext(e => m)
            );
        }


        [Test]
        public void Handler_Fallback_Error()
        {
            MaybeSource.Error<int>(new InvalidOperationException("main"))
                .OnErrorResumeNext(e => MaybeSource.Error<int>(new InvalidOperationException("fallback")))
                .Test()
                .AssertFailure(typeof(InvalidOperationException))
                .AssertError(typeof(InvalidOperationException), "fallback");
        }

        [Test]
        public void Handler_Crash()
        {
            MaybeSource.Error<int>(new InvalidOperationException("main"))
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
