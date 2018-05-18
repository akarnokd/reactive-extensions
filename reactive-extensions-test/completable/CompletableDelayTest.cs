using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Reactive.Concurrency;

namespace akarnokd.reactive_extensions_test.completable
{
    [TestFixture]
    public class CompletableDelayTest
    {
        [Test]
        public void Basic()
        {
            CompletableSource.Empty()
                .Delay(TimeSpan.FromMilliseconds(100), NewThreadScheduler.Default)
                .Test()
                .AwaitDone(TimeSpan.FromSeconds(5))
                .AssertResult();
        }

        [Test]
        public void Error()
        {
            CompletableSource.Error(new InvalidOperationException())
                .Delay(TimeSpan.FromMilliseconds(100), NewThreadScheduler.Default)
                .Test()
                .AwaitDone(TimeSpan.FromSeconds(5))
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Dispose()
        {
            var cs = new CompletableSubject();

            var to = cs.Delay(TimeSpan.FromMinutes(5), NewThreadScheduler.Default)
                .Test();

            Assert.True(cs.HasObserver());

            to.Dispose();

            Assert.False(cs.HasObserver());
        }

        [Test]
        public void Basic_Step()
        {
            var ts = new TestScheduler();

            var to = CompletableSource.Empty()
                .Delay(TimeSpan.FromSeconds(1), ts).Test();

            to.AssertEmpty();

            ts.AdvanceTimeBy(500);

            to.AssertEmpty();

            ts.AdvanceTimeBy(500);

            to.AssertResult();
        }

        [Test]
        public void Error_Step()
        {
            var ts = new TestScheduler();

            var to = CompletableSource.Error(new InvalidOperationException())
                .Delay(TimeSpan.FromSeconds(1), ts).Test();

            to.AssertEmpty();

            ts.AdvanceTimeBy(500);

            to.AssertEmpty();

            ts.AdvanceTimeBy(500);

            to.AssertFailure(typeof(InvalidOperationException));
        }
    }
}
