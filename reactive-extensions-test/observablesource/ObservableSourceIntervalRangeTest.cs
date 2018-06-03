using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Reactive.Concurrency;

namespace akarnokd.reactive_extensions_test.observablesource
{
    [TestFixture]
    public class ObservableSourceIntervalRangeTest
    {
        [Test]
        public void Regular_Infinite()
        {
            ObservableSource.Interval(TimeSpan.FromMilliseconds(1), ThreadPoolScheduler.Instance)
                .Take(10)
                .Test()
                .AwaitDone(TimeSpan.FromSeconds(5))
                .AssertResult(0, 1, 2, 3, 4, 5, 6, 7, 8, 9);
        }

        [Test]
        public void Regular_Infinite_InitialDelay()
        {
            ObservableSource.Interval(TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(1), ThreadPoolScheduler.Instance)
                .Take(10)
                .Test()
                .AwaitDone(TimeSpan.FromSeconds(5))
                .AssertResult(0, 1, 2, 3, 4, 5, 6, 7, 8, 9);
        }

        [Test]
        public void Regular_Finite()
        {
            ObservableSource.IntervalRange(1, 5, TimeSpan.FromMilliseconds(1), ThreadPoolScheduler.Instance)
                .Test()
                .AwaitDone(TimeSpan.FromSeconds(5))
                .AssertResult(1, 2, 3, 4, 5);
        }

        [Test]
        public void Regular_Finite_InitialDelay()
        {
            ObservableSource.IntervalRange(1, 5, TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(1), ThreadPoolScheduler.Instance)
                .Test()
                .AwaitDone(TimeSpan.FromSeconds(5))
                .AssertResult(1, 2, 3, 4, 5);
        }

        [Test]
        public void Regular_Finite_Empty()
        {
            ObservableSource.IntervalRange(1, 0, TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(1), ThreadPoolScheduler.Instance)
                .Test()
                .AwaitDone(TimeSpan.FromSeconds(5))
                .AssertResult();
        }

        [Test]
        public void Regular_Step()
        {
            var ts = new TestScheduler();

            var to = ObservableSource.IntervalRange(1, 5, TimeSpan.FromMilliseconds(2), ts)
                .Test();

            to.AssertEmpty();

            ts.AdvanceTimeBy(1);

            to.AssertEmpty();

            ts.AdvanceTimeBy(1);

            to.AssertValuesOnly(1);

            ts.AdvanceTimeBy(4);

            to.AssertValuesOnly(1, 2, 3);

            ts.AdvanceTimeBy(4);

            to.AssertResult(1, 2, 3, 4, 5);
        }

        [Test]
        public void Regular_Step_InitialDelay()
        {
            var ts = new TestScheduler();

            var to = ObservableSource.IntervalRange(1, 5, TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(1), ts)
                .Test();

            to.AssertEmpty();

            ts.AdvanceTimeBy(5);

            to.AssertEmpty();

            ts.AdvanceTimeBy(5);

            to.AssertValuesOnly(1);

            ts.AdvanceTimeBy(2);

            to.AssertValuesOnly(1, 2, 3);

            ts.AdvanceTimeBy(2);

            to.AssertResult(1, 2, 3, 4, 5);
        }

        [Test]
        public void Fused_Rejected()
        {
            ObservableSource.Interval(TimeSpan.FromMilliseconds(1), ThreadPoolScheduler.Instance)
                .Test(fusionMode: FusionSupport.Sync)
                .AssertFuseable()
                .AssertFusionMode(FusionSupport.None)
                .Dispose();
        }

        [Test]
        public void Fused_Finite()
        {
            ObservableSource.IntervalRange(1, 5, TimeSpan.FromMilliseconds(1), ThreadPoolScheduler.Instance)
                .Test(fusionMode: FusionSupport.Any)
                .AssertFuseable()
                .AssertFusionMode(FusionSupport.Async)
                .AwaitDone(TimeSpan.FromSeconds(5))
                .AssertResult(1, 2, 3, 4, 5);
        }

        [Test]
        public void Fused_Finite_InitialDelay()
        {
            ObservableSource.IntervalRange(1, 5, TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(1), ThreadPoolScheduler.Instance)
                .Test(fusionMode: FusionSupport.Any)
                .AssertFuseable()
                .AssertFusionMode(FusionSupport.Async)
                .AwaitDone(TimeSpan.FromSeconds(5))
                .AssertResult(1, 2, 3, 4, 5);
        }

        [Test]
        public void Fused_Finite_Empty()
        {
            ObservableSource.IntervalRange(1, 0, TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(1), ThreadPoolScheduler.Instance)
                .Test(fusionMode: FusionSupport.Any)
                .AssertFuseable()
                .AssertFusionMode(FusionSupport.Async)
                .AwaitDone(TimeSpan.FromSeconds(5))
                .AssertResult();
        }

        [Test]
        public void Fused_Step()
        {
            var ts = new TestScheduler();

            var to = ObservableSource.IntervalRange(1, 5, TimeSpan.FromMilliseconds(2), ts)
                .Test(fusionMode: FusionSupport.Any)
                .AssertFuseable()
                .AssertFusionMode(FusionSupport.Async);

            to.AssertEmpty();

            ts.AdvanceTimeBy(1);

            to.AssertEmpty();

            ts.AdvanceTimeBy(1);

            to.AssertValuesOnly(1);

            ts.AdvanceTimeBy(4);

            to.AssertValuesOnly(1, 2, 3);

            ts.AdvanceTimeBy(4);

            to.AssertResult(1, 2, 3, 4, 5);
        }

        [Test]
        public void Fused_Step_InitialDelay()
        {
            var ts = new TestScheduler();

            var to = ObservableSource.IntervalRange(1, 5, TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(1), ts)
                .Test(fusionMode: FusionSupport.Any)
                .AssertFuseable()
                .AssertFusionMode(FusionSupport.Async);

            to.AssertEmpty();

            ts.AdvanceTimeBy(5);

            to.AssertEmpty();

            ts.AdvanceTimeBy(5);

            to.AssertValuesOnly(1);

            ts.AdvanceTimeBy(2);

            to.AssertValuesOnly(1, 2, 3);

            ts.AdvanceTimeBy(2);

            to.AssertResult(1, 2, 3, 4, 5);
        }
    }
}
