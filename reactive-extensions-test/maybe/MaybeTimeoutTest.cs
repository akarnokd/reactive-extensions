using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Reactive.Concurrency;

namespace akarnokd.reactive_extensions_test.maybe
{
    [TestFixture]
    public class MaybeTimeoutTest
    {

        [Test]
        public void Success()
        {
            MaybeSource.Just(1)
                .Timeout(TimeSpan.FromMinutes(1), NewThreadScheduler.Default)
                .Test()
                .AssertResult(1);
        }

        [Test]
        public void Success_Fallback()
        {
            var count = 0;
            var fb = MaybeSource.FromAction<int>(() => count++);

            MaybeSource.Just(1)
                .Timeout(TimeSpan.FromMinutes(1), NewThreadScheduler.Default, fb)
                .Test()
                .AssertResult(1);

            Assert.AreEqual(0, count);
        }

        [Test]
        public void Empty()
        {
            MaybeSource.Empty<int>()
                .Timeout(TimeSpan.FromMinutes(1), NewThreadScheduler.Default)
                .Test()
                .AssertResult();
        }

        [Test]
        public void Empty_Fallback()
        {
            var count = 0;
            var fb = MaybeSource.FromAction<int>(() => count++);

            MaybeSource.Empty<int>()
                .Timeout(TimeSpan.FromMinutes(1), NewThreadScheduler.Default, fb)
                .Test()
                .AssertResult();

            Assert.AreEqual(0, count);
        }

        [Test]
        public void Error()
        {
            MaybeSource.Error<int>(new InvalidOperationException())
                .Timeout(TimeSpan.FromMinutes(1), NewThreadScheduler.Default)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Error_Fallback()
        {
            var count = 0;
            var fb = MaybeSource.FromAction<int>(() => count++);

            MaybeSource.Error<int>(new InvalidOperationException())
                .Timeout(TimeSpan.FromMinutes(1), NewThreadScheduler.Default, fb)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(0, count);
        }

        [Test]
        public void No_Timeout()
        {
            var ts = new TestScheduler();
            var us = new MaybeSubject<int>();

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
            var us = new MaybeSubject<int>();

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
            var us = new MaybeSubject<int>();

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
        public void Timeout_Fallback_Empty()
        {
            var ts = new TestScheduler();
            var us = new MaybeSubject<int>();

            var count = 0;
            var fb = MaybeSource.FromAction<int>(() => count++);

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
        public void Timeout_Fallback_Success()
        {
            var ts = new TestScheduler();
            var us = new MaybeSubject<int>();

            var count = 0;
            var fb = MaybeSource.FromFunc<int>(() => ++count);

            var to = us
                .Timeout(TimeSpan.FromSeconds(1), ts, fb)
                .Test();

            to.AssertEmpty();

            ts.AdvanceTimeBy(100);

            Assert.True(us.HasObserver());

            ts.AdvanceTimeBy(900);

            Assert.False(us.HasObserver());

            to.AssertResult(1);

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Timeout_Fallback_Error()
        {
            var ts = new TestScheduler();
            var us = new MaybeSubject<int>();

            var count = 0;
            var fb = MaybeSource.FromFunc<int>(() => 
            {
                ++count;
                throw new InvalidOperationException();
            });

            var to = us
                .Timeout(TimeSpan.FromSeconds(1), ts, fb)
                .Test();

            to.AssertEmpty();

            ts.AdvanceTimeBy(100);

            Assert.True(us.HasObserver());

            ts.AdvanceTimeBy(900);

            Assert.False(us.HasObserver());

            to.AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Dispose_Main()
        {
            var ts = new TestScheduler();
            var us = new MaybeSubject<int>();

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
            var us = new MaybeSubject<int>();

            var to = MaybeSource.Never<int>()
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
