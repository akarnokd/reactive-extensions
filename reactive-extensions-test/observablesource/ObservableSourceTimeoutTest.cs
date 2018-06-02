using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.observablesource
{
    [TestFixture]
    public class ObservableSourceTimeoutTest
    {
        [Test]
        public void Timed_Basic()
        {
            var ts = new TestScheduler();

            ObservableSource.Range(1, 5)
                .Timeout(TimeSpan.FromSeconds(1), ts)
                .Test()
                .AssertResult(1, 2, 3, 4, 5);
        }

        [Test]
        public void Timed_Fallback()
        {
            var ts = new TestScheduler();

            ObservableSource.Range(1, 5)
                .Timeout(TimeSpan.FromSeconds(1), ts, ObservableSource.Range(6, 5))
                .Test()
                .AssertResult(1, 2, 3, 4, 5);
        }

        [Test]
        public void Timed_Timeout()
        {
            var ts = new TestScheduler();

            var to = ObservableSource.Never<int>()
                .Timeout(TimeSpan.FromSeconds(1), ts)
                .Test();

            to.AssertEmpty();

            ts.AdvanceTimeBy(1000);

            to.AssertFailure(typeof(TimeoutException));
        }

        [Test]
        public void Timed_Timeout_Fallback()
        {
            var ts = new TestScheduler();

            var to = ObservableSource.Never<int>()
                .Timeout(TimeSpan.FromSeconds(1), ts, ObservableSource.Range(6, 5))
                .Test();

            to.AssertEmpty();

            ts.AdvanceTimeBy(1000);

            to.AssertResult(6, 7, 8, 9, 10);
        }

        [Test]
        public void Timed_Main_Error()
        {
            var ts = new TestScheduler();

            ObservableSource.Error<int>(new InvalidOperationException())
                .Timeout(TimeSpan.FromSeconds(1), ts)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Timed_Fallback_Error()
        {
            var ts = new TestScheduler();

            var to = ObservableSource.Never<int>()
                .Timeout(TimeSpan.FromSeconds(1), ts, ObservableSource.Error<int>(new InvalidOperationException()))
                .Test();

            to.AssertEmpty();

            ts.AdvanceTimeBy(1000);

            to.AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Timed_Dispose_Main()
        {
            var ts = new TestScheduler();

            var source = new PublishSubject<int>();

            var to = source.Timeout(TimeSpan.FromSeconds(1), ts)
                .Test();

            Assert.True(source.HasObservers);

            to.Dispose();

            Assert.False(source.HasObservers);
        }


        [Test]
        public void Timed_Dispose_Other()
        {
            var ts = new TestScheduler();

            var source = new PublishSubject<int>();

            var to = ObservableSource.Never<int>()
                .Timeout(TimeSpan.FromSeconds(1), ts, source)
                .Test();

            Assert.False(source.HasObservers);

            ts.AdvanceTimeBy(1000);

            Assert.True(source.HasObservers);

            to.Dispose();

            Assert.False(source.HasObservers);
        }
    }
}
