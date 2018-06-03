using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Reactive.Concurrency;

namespace akarnokd.reactive_extensions_test.observablesource
{
    [TestFixture]
    public class ObservableSourceTimerTest
    {
        [Test]
        public void Regular_Basic()
        {
            ObservableSource.Timer(TimeSpan.FromMilliseconds(100), ThreadPoolScheduler.Instance)
                .Test()
                .AwaitDone(TimeSpan.FromSeconds(5))
                .AssertResult(0L);
        }

        [Test]
        public void Regular_Basic2()
        {
            var ts = new TestScheduler();

            var to = ObservableSource.Timer(TimeSpan.FromSeconds(1), ts)
                .Test();

            to.AssertEmpty();

            ts.AdvanceTimeBy(500);

            to.AssertEmpty();

            ts.AdvanceTimeBy(500);

            to.AssertResult(0L);
        }

        [Test]
        public void Regular_Dispose()
        {
            var ts = new TestScheduler();

            var to = ObservableSource.Timer(TimeSpan.FromSeconds(1), ts)
                .Test();

            to.AssertEmpty();

            ts.AdvanceTimeBy(500);

            to.Dispose();

            ts.AdvanceTimeBy(500);

            to.AssertEmpty();
        }

        [Test]
        public void Fused_Basic()
        {
            ObservableSource.Timer(TimeSpan.FromMilliseconds(100), ThreadPoolScheduler.Instance)
                .Test(fusionMode: FusionSupport.Any)
                .AssertFuseable()
                .AssertFusionMode(FusionSupport.Async)
                .AwaitDone(TimeSpan.FromSeconds(5))
                .AssertResult(0L);
        }

        [Test]
        public void Fused_Rejected()
        {
            ObservableSource.Timer(TimeSpan.FromMilliseconds(100), ThreadPoolScheduler.Instance)
                .Test(fusionMode: FusionSupport.Sync)
                .AssertFuseable()
                .AssertFusionMode(FusionSupport.None)
                .AwaitDone(TimeSpan.FromSeconds(5))
                .AssertResult(0L);
        }

        [Test]
        public void Fused_Basic2()
        {
            var ts = new TestScheduler();

            var to = ObservableSource.Timer(TimeSpan.FromSeconds(1), ts)
                .Test(fusionMode: FusionSupport.Any)
                .AssertFuseable()
                .AssertFusionMode(FusionSupport.Async);

            to.AssertEmpty();

            ts.AdvanceTimeBy(500);

            to.AssertEmpty();

            ts.AdvanceTimeBy(500);

            to.AssertResult(0L);
        }

        [Test]
        public void Fused_Dispose()
        {
            var ts = new TestScheduler();

            var to = ObservableSource.Timer(TimeSpan.FromSeconds(1), ts)
                .Test(fusionMode: FusionSupport.Any)
                .AssertFuseable()
                .AssertFusionMode(FusionSupport.Async);

            to.AssertEmpty();

            ts.AdvanceTimeBy(500);

            to.Dispose();

            ts.AdvanceTimeBy(500);

            to.AssertEmpty();
        }
    }
}
