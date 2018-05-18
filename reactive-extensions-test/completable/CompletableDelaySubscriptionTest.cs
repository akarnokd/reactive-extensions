using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Reactive.Concurrency;

namespace akarnokd.reactive_extensions_test.completable
{
    [TestFixture]
    public class CompletableDelaySubscriptionTest
    {
        [Test]
        public void Time_Basic()
        {
            CompletableSource.Empty()
                .DelaySubscription(TimeSpan.FromMilliseconds(100), NewThreadScheduler.Default)
                .Test()
                .AwaitDone(TimeSpan.FromSeconds(5))
                .AssertResult();
        }

        [Test]
        public void Time_Error()
        {
            CompletableSource.Error(new InvalidOperationException())
                .DelaySubscription(TimeSpan.FromMilliseconds(100), NewThreadScheduler.Default)
                .Test()
                .AwaitDone(TimeSpan.FromSeconds(5))
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Time_Dispose()
        {
            var ts = new TestScheduler();

            var to = CompletableSource.Empty()
                .DelaySubscription(TimeSpan.FromSeconds(1), ts)
                .Test();

            ts.AdvanceTimeBy(500);

            to.Dispose();

            ts.AdvanceTimeBy(500);

            to.AssertEmpty();
        }

        [Test]
        public void Time_Dispose_Other()
        {
            var ts = new TestScheduler();

            var cs = new CompletableSubject();

            var to = cs
                .DelaySubscription(TimeSpan.FromSeconds(1), ts)
                .Test();

            Assert.False(cs.HasObserver());

            ts.AdvanceTimeBy(500);

            Assert.False(cs.HasObserver());

            ts.AdvanceTimeBy(500);

            Assert.True(cs.HasObserver());

            to.Dispose();

            Assert.False(cs.HasObserver());

            to.AssertEmpty();
        }

        [Test]
        public void Other_Basic()
        {
            CompletableSource.Empty()
                .DelaySubscription(CompletableSource.Timer(TimeSpan.FromMilliseconds(100), NewThreadScheduler.Default))
                .Test()
                .AwaitDone(TimeSpan.FromSeconds(5))
                .AssertResult();
        }

        [Test]
        public void Other_Error()
        {
            CompletableSource.Error(new InvalidOperationException())
                .DelaySubscription(CompletableSource.Timer(TimeSpan.FromMilliseconds(100), NewThreadScheduler.Default))
                .Test()
                .AwaitDone(TimeSpan.FromSeconds(5))
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Other_Dispose()
        {
            var ts = new CompletableSubject();

            var to = CompletableSource.Empty()
                .DelaySubscription(ts)
                .Test();

            Assert.True(ts.HasObserver());

            to.Dispose();

            Assert.False(ts.HasObserver());

            to.AssertEmpty();
        }

        [Test]
        public void Other_Dispose_Other()
        {
            var ts = new CompletableSubject();

            var cs = new CompletableSubject();

            var to = cs
                .DelaySubscription(ts)
                .Test();

            Assert.False(cs.HasObserver());

            ts.OnCompleted();

            Assert.True(cs.HasObserver());

            to.Dispose();

            Assert.False(cs.HasObserver());

            to.AssertEmpty();
        }
    }
}
