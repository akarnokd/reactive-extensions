using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Reactive.Concurrency;

namespace akarnokd.reactive_extensions_test.single
{
    [TestFixture]
    public class SingleDelaySubscriptionTest
    {
        [Test]
        public void Time_Success()
        {
            SingleSource.Just(1)
                .DelaySubscription(TimeSpan.FromMilliseconds(100), NewThreadScheduler.Default)
                .Test()
                .AwaitDone(TimeSpan.FromSeconds(5))
                .AssertResult(1);
        }

        [Test]
        public void Time_Error()
        {
            SingleSource.Error<int>(new InvalidOperationException())
                .DelaySubscription(TimeSpan.FromMilliseconds(100), NewThreadScheduler.Default)
                .Test()
                .AwaitDone(TimeSpan.FromSeconds(5))
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Time_Dispose()
        {
            var ts = new TestScheduler();

            var to = SingleSource.Just(1)
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

            var cs = new SingleSubject<int>();

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
            SingleSource.Just(1)
                .DelaySubscription(SingleSource.Timer(TimeSpan.FromMilliseconds(100), NewThreadScheduler.Default))
                .Test()
                .AwaitDone(TimeSpan.FromSeconds(5))
                .AssertResult(1);
        }

        [Test]
        public void Other_Success_Success()
        {
            SingleSource.Just(1)
                .DelaySubscription(SingleSource.Just<string>(""))
                .Test()
                .AwaitDone(TimeSpan.FromSeconds(5))
                .AssertResult(1);
        }

        [Test]
        public void Other_Success()
        {
            SingleSource.Just(1)
                .DelaySubscription(SingleSource.Timer(TimeSpan.FromMilliseconds(100), NewThreadScheduler.Default))
                .Test()
                .AwaitDone(TimeSpan.FromSeconds(5))
                .AssertResult(1);
        }

        [Test]
        public void Other_Error()
        {
            SingleSource.Error<int>(new InvalidOperationException())
                .DelaySubscription(SingleSource.Timer(TimeSpan.FromMilliseconds(100), NewThreadScheduler.Default))
                .Test()
                .AwaitDone(TimeSpan.FromSeconds(5))
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Other_Delay_Error()
        {
            SingleSource.Error<int>(new NullReferenceException())
                .DelaySubscription(SingleSource.Error<int>(new InvalidOperationException()))
                .Test()
                .AwaitDone(TimeSpan.FromSeconds(5))
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Other_Dispose()
        {
            var ts = new SingleSubject<int>();

            var to = SingleSource.Just(1)
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
            var ts = new SingleSubject<int>();

            var cs = new SingleSubject<int>();

            var to = cs
                .DelaySubscription(ts)
                .Test();

            Assert.False(cs.HasObserver());

            ts.OnSuccess(1);

            Assert.True(cs.HasObserver());

            to.Dispose();

            Assert.False(cs.HasObserver());

            to.AssertEmpty();
        }
    }
}
