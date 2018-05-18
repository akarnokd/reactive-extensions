using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Reactive.Concurrency;

namespace akarnokd.reactive_extensions_test.completable
{
    [TestFixture]
    public class CompletableTimeoutTest
    {
        [Test]
        public void Basic()
        {
            CompletableSource.Empty()
                .Timeout(TimeSpan.FromMinutes(1), NewThreadScheduler.Default)
                .Test()
                .AssertResult();
        }

        [Test]
        public void Basic_Fallback()
        {
            var count = 0;
            var fb = CompletableSource.FromAction(() => count++);

            CompletableSource.Empty()
                .Timeout(TimeSpan.FromMinutes(1), NewThreadScheduler.Default, fb)
                .Test()
                .AssertResult();

            Assert.AreEqual(0, count);
        }

        [Test]
        public void Error()
        {
            CompletableSource.Error(new InvalidOperationException())
                .Timeout(TimeSpan.FromMinutes(1), NewThreadScheduler.Default)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Error_Fallback()
        {
            var count = 0;
            var fb = CompletableSource.FromAction(() => count++);

            CompletableSource.Error(new InvalidOperationException())
                .Timeout(TimeSpan.FromMinutes(1), NewThreadScheduler.Default, fb)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(0, count);
        }

        [Test]
        public void No_Timeout()
        {
            var ts = new TestScheduler();
            var us = new CompletableSubject();

            var to = us
                .Timeout(TimeSpan.FromSeconds(1), ts)
                .Test();

            to.AssertEmpty();

            ts.AdvanceTimeBy(100);

            Assert.True(us.HasObserver());

            us.OnCompleted();

            ts.AdvanceTimeBy(900);

            to.AssertResult();
        }

        [Test]
        public void No_Timeout_Error()
        {
            var ts = new TestScheduler();
            var us = new CompletableSubject();

            var to = us
                .Timeout(TimeSpan.FromSeconds(1), ts)
                .Test();

            to.AssertEmpty();

            ts.AdvanceTimeBy(100);

            Assert.True(us.HasObserver());

            us.OnError(new InvalidOperationException());

            ts.AdvanceTimeBy(900);

            to.AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Timeout()
        {
            var ts = new TestScheduler();
            var us = new CompletableSubject();

            var to = us
                .Timeout(TimeSpan.FromSeconds(1), ts)
                .Test();

            to.AssertEmpty();

            ts.AdvanceTimeBy(100);

            Assert.True(us.HasObserver());

            ts.AdvanceTimeBy(900);

            Assert.False(us.HasObserver());

            to.AssertFailure(typeof(TimeoutException));
        }

        [Test]
        public void Timeout_Fallback()
        {
            var ts = new TestScheduler();
            var us = new CompletableSubject();

            var count = 0;
            var fb = CompletableSource.FromAction(() => count++);

            var to = us
                .Timeout(TimeSpan.FromSeconds(1), ts, fb)
                .Test();

            to.AssertEmpty();

            ts.AdvanceTimeBy(100);

            Assert.True(us.HasObserver());

            ts.AdvanceTimeBy(900);

            Assert.False(us.HasObserver());

            to.AssertResult();

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Dispose_Main()
        {
            var ts = new TestScheduler();
            var us = new CompletableSubject();

            var to = us
            .Timeout(TimeSpan.FromSeconds(1), ts)
            .Test();

            Assert.True(us.HasObserver());

            to.Dispose();

            Assert.False(us.HasObserver());

            ts.AdvanceTimeBy(1000);

            to.AssertEmpty();
        }


        [Test]
        public void Dispose_Fallback()
        {
            var ts = new TestScheduler();
            var us = new CompletableSubject();

            var to = CompletableSource.Never()
            .Timeout(TimeSpan.FromSeconds(1), ts, us)
            .Test();

            ts.AdvanceTimeBy(100);

            Assert.False(us.HasObserver());

            ts.AdvanceTimeBy(900);

            Assert.True(us.HasObserver());

            to.Dispose();

            Assert.False(us.HasObserver());

            to.AssertEmpty();
        }
    }
}
