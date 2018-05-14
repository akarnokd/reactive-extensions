using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test
{
    [TestFixture]
    public class TestSchedulerTest
    {
        [Test]
        public void Basic()
        {
            var scheduler = new TestScheduler(1);

            Assert.AreEqual(scheduler.Now, DateTimeOffset.FromUnixTimeMilliseconds(1));

            Assert.False(scheduler.HasTasks());

            var sad = new SingleAssignmentDisposable();

            var d = scheduler.Schedule(sad, (sch, state) => state);

            Assert.True(scheduler.HasTasks());

            scheduler.RunAll();

            Assert.False(scheduler.HasTasks());

            Assert.False(sad.IsDisposed());

            d.Dispose();

            Assert.True(sad.IsDisposed());
        }

        [Test]
        public void Basic_Timed()
        {
            var scheduler = new TestScheduler(1);

            Assert.AreEqual(scheduler.Now, DateTimeOffset.FromUnixTimeMilliseconds(1));

            Assert.False(scheduler.HasTasks());

            var count = 0;

            scheduler.Schedule((object)null, TimeSpan.FromSeconds(1), (sch, state) => {
                count++;
                return DisposableHelper.EMPTY;
            });

            Assert.True(scheduler.HasTasks());

            Assert.AreEqual(0, count);

            scheduler.AdvanceTimeBy(500);

            Assert.AreEqual(0, count);

            scheduler.AdvanceTimeBy(500);

            Assert.AreEqual(1, count);

            Assert.False(scheduler.HasTasks());
        }

        [Test]
        public void Basic_Timed_Disposed()
        {
            var scheduler = new TestScheduler(1);

            Assert.AreEqual(scheduler.Now, DateTimeOffset.FromUnixTimeMilliseconds(1));

            Assert.False(scheduler.HasTasks());

            var count = 0;

            var d = scheduler.Schedule((object)null, TimeSpan.FromSeconds(1), (sch, state) => {
                count++;
                return DisposableHelper.EMPTY;
            });

            Assert.True(scheduler.HasTasks());

            Assert.AreEqual(0, count);

            scheduler.AdvanceTimeBy(500);

            Assert.AreEqual(0, count);

            d.Dispose();

            scheduler.AdvanceTimeBy(500);

            Assert.AreEqual(0, count);

            Assert.False(scheduler.HasTasks());
        }

        [Test]
        public void Negative_Timespan()
        {

            var scheduler = new TestScheduler(1);

            var count = 0;

            scheduler.Schedule((object)null, TimeSpan.FromSeconds(-1), (sch, state) => {
                count++;
                return DisposableHelper.EMPTY;
            });

            scheduler.AdvanceTimeBy(500);

            Assert.AreEqual(1, count);

            Assert.False(scheduler.HasTasks());
        }

        [Test]
        public void Negative_Past_DateTimeOffset()
        {

            var scheduler = new TestScheduler(10000);

            var count = 0;

            scheduler.Schedule((object)null, DateTimeOffset.FromUnixTimeMilliseconds(0), (sch, state) => {
                count++;
                return DisposableHelper.EMPTY;
            });

            scheduler.AdvanceTimeBy(500);

            Assert.AreEqual(1, count);

            Assert.False(scheduler.HasTasks());
        }
    }
}
