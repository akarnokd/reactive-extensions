using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Reactive.Concurrency;

namespace akarnokd.reactive_extensions_test.single
{
    [TestFixture]
    public class SingleTimerTest
    {
        [Test]
        public void Basic()
        {
            SingleSource.Timer(TimeSpan.FromMilliseconds(100), NewThreadScheduler.Default)
                .Test()
                .AwaitDone(TimeSpan.FromSeconds(5))
                .AssertResult(0L);
        }

        [Test]
        public void Dispose()
        {
            var sch = new TestScheduler();
            var to = SingleSource.Timer(TimeSpan.FromMilliseconds(100), sch)
                .Test();

            Assert.True(sch.HasTasks());

            sch.AdvanceTimeBy(50);

            Assert.True(sch.HasTasks());
            to.AssertEmpty();

            sch.AdvanceTimeBy(50);

            Assert.False(sch.HasTasks());
            to.AssertResult(0L);
        }
    }
}
