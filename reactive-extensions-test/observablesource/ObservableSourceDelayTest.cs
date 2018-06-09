using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Reactive.Concurrency;

namespace akarnokd.reactive_extensions_test.observablesource
{
    [TestFixture]
    public class ObservableSourceDelayTest
    {
        [Test]
        public void Time_Basic()
        {
            ObservableSource.Range(1, 5)
                .Delay(TimeSpan.FromMilliseconds(10), ThreadPoolScheduler.Instance)
                .Test()
                .AwaitDone(TimeSpan.FromSeconds(5))
                .AssertResult(1, 2, 3, 4, 5);
        }

        [Test]
        public void Time_Error_Delayed()
        {
            ObservableSource.Range(1, 5).ConcatError(new InvalidOperationException())
                .Delay(TimeSpan.FromMilliseconds(10), ThreadPoolScheduler.Instance, true)
                .Test()
                .AwaitDone(TimeSpan.FromSeconds(5))
                .AssertFailure(typeof(InvalidOperationException), 1, 2, 3, 4, 5);
        }

        [Test]
        public void Time_Error()
        {
            ObservableSource.Error<int>(new InvalidOperationException())
                .Delay(TimeSpan.FromMilliseconds(10), ThreadPoolScheduler.Instance, false)
                .Test()
                .AwaitDone(TimeSpan.FromSeconds(5))
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Time_Dispose()
        {
            TestHelper.VerifyDisposeObservableSource<int, int>(o => o.Delay(TimeSpan.FromMilliseconds(1), ThreadPoolScheduler.Instance));
        }

        [Test]
        public void Time_Step()
        {
            var ts = new TestScheduler();

            var subj = new PublishSubject<int>();

            var to = subj.Delay(TimeSpan.FromSeconds(1), ts).Test();

            subj.OnNext(1);
            subj.OnNext(2);

            Assert.True(ts.HasTasks());

            to.AssertEmpty();

            ts.AdvanceTimeBy(1000);

            to.AssertValuesOnly(1, 2);

            ts.AdvanceTimeBy(1000);

            to.AssertValuesOnly(1, 2);

            subj.OnNext(3);

            ts.AdvanceTimeBy(200);

            subj.OnNext(4);

            ts.AdvanceTimeBy(200);

            subj.OnNext(5);

            ts.AdvanceTimeBy(200);

            to.AssertValuesOnly(1, 2);

            ts.AdvanceTimeBy(400);

            to.AssertValuesOnly(1, 2, 3);

            ts.AdvanceTimeBy(200);

            to.AssertValuesOnly(1, 2, 3, 4);

            ts.AdvanceTimeBy(200);

            to.AssertValuesOnly(1, 2, 3, 4, 5);

            subj.OnCompleted();

            ts.AdvanceTimeBy(500);

            to.AssertValuesOnly(1, 2, 3, 4, 5);

            ts.AdvanceTimeBy(500);

            to.AssertResult(1, 2, 3, 4, 5);

            Assert.False(ts.HasTasks());
        }

        [Test]
        public void Time_Step_Error()
        {
            var ts = new TestScheduler();

            var subj = new PublishSubject<int>();

            var to = subj.Delay(TimeSpan.FromSeconds(1), ts, false).Test();

            subj.OnNext(1);
            subj.OnNext(2);
            subj.OnError(new InvalidOperationException());

            Assert.True(ts.HasTasks());

            to.AssertEmpty();

            ts.AdvanceTimeBy(1000);

            to.AssertFailure(typeof(InvalidOperationException));
        }


        [Test]
        public void Time_Step_Error_Delayed()
        {
            var ts = new TestScheduler();

            var subj = new PublishSubject<int>();

            var to = subj.Delay(TimeSpan.FromSeconds(1), ts, true).Test();

            subj.OnNext(1);
            subj.OnNext(2);
            subj.OnError(new InvalidOperationException());

            Assert.True(ts.HasTasks());

            to.AssertEmpty();

            ts.AdvanceTimeBy(1000);

            to.AssertFailure(typeof(InvalidOperationException), 1, 2);
        }
    }
}
