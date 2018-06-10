using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Reactive.Concurrency;

namespace akarnokd.reactive_extensions_test.observablesource
{
    [TestFixture]
    public class ObservableSourceTakeLastTimedTest
    {
        [Test]
        public void Basic()
        {
            ObservableSource.Range(1, 5)
                .TakeLast(TimeSpan.FromSeconds(5), CurrentThreadScheduler.Instance)
                .Test()
                .AssertResult(1, 2, 3, 4, 5);
        }


        [Test]
        public void Take()
        {
            ObservableSource.Range(1, 5)
                .TakeLast(TimeSpan.FromSeconds(5), CurrentThreadScheduler.Instance)
                .Take(3)
                .Test()
                .AssertResult(1, 2, 3);
        }

        [Test]
        public void Error()
        {
            ObservableSource.Range(1, 5).ConcatError(new InvalidOperationException())
                .TakeLast(TimeSpan.FromSeconds(5), CurrentThreadScheduler.Instance)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Step()
        {
            var subj = new PublishSubject<int>();
            var ts = new TestScheduler();

            var to = subj.TakeLast(TimeSpan.FromMilliseconds(500), ts)
                .Test();

            to.AssertEmpty();

            subj.OnNext(1);

            ts.AdvanceTimeBy(200);

            subj.OnNext(2);

            ts.AdvanceTimeBy(200);

            subj.OnNext(3);

            ts.AdvanceTimeBy(200);

            subj.OnNext(4);

            ts.AdvanceTimeBy(200);

            to.AssertEmpty();

            subj.OnCompleted();

            to.AssertResult(3, 4);
        }

        [Test]
        public void Step_Exact()
        {
            var subj = new PublishSubject<int>();
            var ts = new TestScheduler();

            var to = subj.TakeLast(TimeSpan.FromMilliseconds(500), ts)
                .Test();

            subj.OnNext(1);

            ts.AdvanceTimeBy(500);

            subj.OnNext(2);
            subj.OnCompleted();

            to.AssertResult(2);
        }
    }
}
