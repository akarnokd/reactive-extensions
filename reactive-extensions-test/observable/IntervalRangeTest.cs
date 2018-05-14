using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Reactive.Concurrency;

namespace akarnokd.reactive_extensions_test.observable
{
    [TestFixture]
    public class IntervalRangeTest
    {
        [Test]
        [Timeout(10000)]
        public void Basic()
        {
            var ts = new TestScheduler();

            var to = ReactiveExtensions
                .IntervalRange(0, 5, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), ts)
                .Test();

            to.AssertEmpty();

            ts.RunAll();

            to.AssertResult(0, 1, 2, 3, 4);

            Assert.False(ts.HasTasks());
        }

        [Test]
        [Timeout(10000)]
        public void Basic_Single_Step()
        {
            var ts = new TestScheduler();

            var to = ReactiveExtensions
                .IntervalRange(0, 5, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), ts)
                .Test();

            to.AssertEmpty();

            ts.AdvanceTimeBy(500);

            to.AssertEmpty();

            ts.AdvanceTimeBy(TimeSpan.FromMilliseconds(500));

            to.AssertValuesOnly(0);

            ts.AdvanceTimeBy(TimeSpan.FromMilliseconds(1000));

            to.AssertValuesOnly(0);

            ts.AdvanceTimeBy(TimeSpan.FromMilliseconds(2000));

            to.AssertValuesOnly(0, 1);

            ts.AdvanceTimeBy(2000);

            to.AssertValuesOnly(0, 1, 2);

            ts.AdvanceTimeBy(TimeSpan.FromMilliseconds(2000));

            to.AssertValuesOnly(0, 1, 2, 3);

            ts.AdvanceTimeBy(TimeSpan.FromMilliseconds(2000));

            to.AssertResult(0, 1, 2, 3, 4);

            Assert.False(ts.HasTasks());
        }
    }
}
