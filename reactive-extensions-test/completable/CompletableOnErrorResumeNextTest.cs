using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.completable
{
    [TestFixture]
    public class CompletableOnErrorResumeNextTest
    {
        [Test]
        public void Basic()
        {
            var count = 0;
            var fb = CompletableSource.FromAction(() => count++);

            CompletableSource.Empty()
                .OnErrorResumeNext(fb)
                .Test()
                .AssertResult();

            Assert.AreEqual(0, count);
        }

        [Test]
        public void Error()
        {
            var count = 0;
            var fb = CompletableSource.FromAction(() => count++);

            CompletableSource.Error(new InvalidOperationException())
                .OnErrorResumeNext(fb)
                .Test()
                .AssertResult();

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Fallback_Error()
        {
            CompletableSource.Error(new InvalidOperationException("main"))
                .OnErrorResumeNext(CompletableSource.Error(new InvalidOperationException("fallback")))
                .Test()
                .AssertFailure(typeof(InvalidOperationException))
                .AssertError(typeof(InvalidOperationException), "fallback");
        }

        [Test]
        public void Dispose_Main()
        {
            var count = 0;
            var fb = CompletableSource.FromAction(() => count++);

            var cs = new CompletableSubject();

            var to = cs
                .OnErrorResumeNext(fb)
                .Test();

            Assert.True(cs.HasObserver());

            to.Dispose();

            Assert.False(cs.HasObserver());

            to.AssertEmpty();

            Assert.AreEqual(0, count);
        }

        [Test]
        public void Dispose_Fallback()
        {
            var cs = new CompletableSubject();

            var to = CompletableSource.Error(new InvalidOperationException())
                .OnErrorResumeNext(cs)
                .Test();

            Assert.True(cs.HasObserver());

            to.Dispose();

            Assert.False(cs.HasObserver());

            to.AssertEmpty();
        }

        [Test]
        public void Handler_Basic()
        {
            var count = 0;
            var fb = CompletableSource.FromAction(() => count++);

            CompletableSource.Empty()
                .OnErrorResumeNext(e => fb)
                .Test()
                .AssertResult();

            Assert.AreEqual(0, count);
        }

        [Test]
        public void Handler_Error()
        {
            var count = 0;
            var fb = CompletableSource.FromAction(() => count++);

            CompletableSource.Error(new InvalidOperationException())
                .OnErrorResumeNext(e => fb)
                .Test()
                .AssertResult();

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Handler_Dispose_Main()
        {
            var count = 0;
            var fb = CompletableSource.FromAction(() => count++);

            var cs = new CompletableSubject();

            var to = cs
                .OnErrorResumeNext(e => fb)
                .Test();

            Assert.True(cs.HasObserver());

            to.Dispose();

            Assert.False(cs.HasObserver());

            to.AssertEmpty();

            Assert.AreEqual(0, count);
        }

        [Test]
        public void Handler_Dispose_Fallback()
        {
            var cs = new CompletableSubject();

            var to = CompletableSource.Error(new InvalidOperationException())
                .OnErrorResumeNext(e => cs)
                .Test();

            Assert.True(cs.HasObserver());

            to.Dispose();

            Assert.False(cs.HasObserver());

            to.AssertEmpty();
        }


        [Test]
        public void Handler_Fallback_Error()
        {
            CompletableSource.Error(new InvalidOperationException("main"))
                .OnErrorResumeNext(e => CompletableSource.Error(new InvalidOperationException("fallback")))
                .Test()
                .AssertFailure(typeof(InvalidOperationException))
                .AssertError(typeof(InvalidOperationException), "fallback");
        }

        [Test]
        public void Handler_Crash()
        {
            CompletableSource.Error(new InvalidOperationException("main"))
                .OnErrorResumeNext(e => { throw new InvalidOperationException("fallback"); })
                .Test()
                .AssertFailure(typeof(AggregateException))
                .AssertCompositeErrorCount(2)
                .AssertCompositeError(0, typeof(InvalidOperationException), "main")
                .AssertCompositeError(1, typeof(InvalidOperationException), "fallback");
        }
    }
}
