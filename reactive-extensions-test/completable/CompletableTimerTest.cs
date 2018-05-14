using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Reactive.Concurrency;

namespace akarnokd.reactive_extensions_test.completable
{
    [TestFixture]
    public class CompletableTimerTest
    {
        [Test]
        public void Basic()
        {
            CompletableSource.Timer(TimeSpan.FromMilliseconds(100), NewThreadScheduler.Default)
                .Test()
                .AwaitDone(TimeSpan.FromSeconds(5))
                .AssertResult();
        }

        [Test]
        public void Dispose()
        {
            var sch = new TestScheduler();
            var to = CompletableSource.Timer(TimeSpan.FromMilliseconds(100), sch)
                .Test();

            Assert.True(sch.HasTasks());

            sch.AdvanceTimeBy(50);

            Assert.True(sch.HasTasks());
            to.AssertEmpty();

            sch.AdvanceTimeBy(50);

            Assert.False(sch.HasTasks());
            to.AssertResult();
        }
    }
}
