using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Reactive.Concurrency;

namespace akarnokd.reactive_extensions_test.observablesource
{
    [TestFixture]
    public class ObservableSourceSkipLastTimedTest
    {
        [Test]
        public void Basic()
        {
            var ts = new TestScheduler();

            var subj = new PublishSubject<int>();

            var to = subj.SkipLast(TimeSpan.FromSeconds(5), ts).Test();

            to.AssertEmpty();

            subj.OnNext(1);

            ts.AdvanceTimeBy(1000);

            to.AssertEmpty();

            subj.OnNext(2);

            ts.AdvanceTimeBy(4000);

            subj.OnNext(3);

            to.AssertValuesOnly(1);

            ts.AdvanceTimeBy(1000);

            subj.OnNext(4);

            to.AssertValuesOnly(1, 2);

            subj.OnCompleted();

            to.AssertResult(1, 2);
        }

        [Test]
        public void Error()
        {
            var ts = new TestScheduler();

            ObservableSource.Range(1, 5).ConcatError(new InvalidOperationException())
                .SkipLast(TimeSpan.FromSeconds(5), ts)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Dispose()
        {
            TestHelper.VerifyDisposeObservableSource<int, int>(o => o.SkipLast(TimeSpan.FromSeconds(1), CurrentThreadScheduler.Instance));
        }
    }
}
