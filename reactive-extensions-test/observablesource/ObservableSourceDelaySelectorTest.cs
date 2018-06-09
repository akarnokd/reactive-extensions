using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.observablesource
{
    [TestFixture]
    public class ObservableSourceDelaySelectorTest
    {
        [Test]
        public void Basic()
        {
            ObservableSource.Range(1, 5)
                .Delay(v => ObservableSource.Range(v, 5))
                .Test()
                .AssertResult(1, 2, 3, 4, 5);
        }

        [Test]
        public void Basic_Empty()
        {
            ObservableSource.Range(1, 5)
                .Delay(v => ObservableSource.Empty<int>())
                .Test()
                .AssertResult(1, 2, 3, 4, 5);
        }

        [Test]
        public void Error()
        {
            ObservableSource.Error<int>(new InvalidOperationException())
                .Delay(v => ObservableSource.Empty<int>())
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Error_Delayed()
        {
            var subj = new PublishSubject<int>();

            var to = ObservableSource.Just(1).ConcatError<int>(new InvalidOperationException())
                .Delay(v => subj, true)
                .Test();

            to.AssertEmpty();

            subj.OnNext(1);

            Assert.False(subj.HasObservers);

            to
                .AssertFailure(typeof(InvalidOperationException), 1);
        }

        [Test]
        public void Error_Inner()
        {
            ObservableSource.Range(1, 5)
                .Delay(v => {
                    if (v == 3)
                    {
                        return ObservableSource.Error<int>(new InvalidOperationException());
                    }
                    return ObservableSource.Empty<int>();
                })
                .Test()
                .AssertFailure(typeof(InvalidOperationException), 1, 2);
        }

        [Test]
        public void Error_Inner_Disposes_Main()
        {
            var subj = new PublishSubject<int>();
            var inner = new PublishSubject<int>();

            var to = subj.Delay(v => inner).Test();

            Assert.True(subj.HasObservers);
            Assert.False(inner.HasObservers);

            to.AssertEmpty();

            subj.OnNext(1);

            to.AssertEmpty();

            Assert.True(subj.HasObservers);
            Assert.True(inner.HasObservers);

            inner.OnError(new InvalidOperationException());

            to.AssertFailure(typeof(InvalidOperationException));

            Assert.False(subj.HasObservers);
            Assert.False(inner.HasObservers);
        }


        [Test]
        public void Error_Main_Disposes_Inner()
        {
            var subj = new PublishSubject<int>();
            var inner = new PublishSubject<int>();

            var to = subj.Delay(v => inner).Test();

            Assert.True(subj.HasObservers);
            Assert.False(inner.HasObservers);

            to.AssertEmpty();

            subj.OnNext(1);

            to.AssertEmpty();

            Assert.True(subj.HasObservers);
            Assert.True(inner.HasObservers);

            subj.OnError(new InvalidOperationException());

            to.AssertFailure(typeof(InvalidOperationException));

            Assert.False(subj.HasObservers);
            Assert.False(inner.HasObservers);
        }

        [Test]
        public void Error_Inner_Delayed()
        {
            ObservableSource.Range(1, 5)
                .Delay(v => {
                    if (v == 3)
                    {
                        return ObservableSource.Error<int>(new InvalidOperationException());
                    }
                    return ObservableSource.Empty<int>();
                }, true)
                .Test()
                .AssertFailure(typeof(InvalidOperationException), 1, 2, 4, 5);
        }

        [Test]
        public void Error_Selector_Throws()
        {
            ObservableSource.Range(1, 5)
                .Delay(v => {
                    if (v == 3)
                    {
                        throw new InvalidOperationException();
                    }
                    return ObservableSource.Empty<int>();
                })
                .Test()
                .AssertFailure(typeof(InvalidOperationException), 1, 2);
        }

        [Test]
        public void Dispose_Main()
        {
            TestHelper.VerifyDisposeObservableSource<int, int>(o => o.Delay(v => ObservableSource.Empty<int>()));
        }

        [Test]
        public void Dispose_Inner()
        {
            TestHelper.VerifyDisposeObservableSource<int, int>(o => ObservableSource.Range(1, 5).Delay(v => o));
        }

        [Test]
        public void Time_Step()
        {
            var ts = new TestScheduler();

            var subj = new PublishSubject<int>();

            var to = subj.Delay(v => ObservableSource.Timer(TimeSpan.FromSeconds(1), ts)).Test();

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

            to.AssertResult(1, 2, 3, 4, 5);

            Assert.False(ts.HasTasks());
        }

        [Test]
        public void Time_Step_Error()
        {
            var ts = new TestScheduler();

            var subj = new PublishSubject<int>();

            var to = subj.Delay(v => ObservableSource.Timer(TimeSpan.FromSeconds(1), ts), false).Test();

            subj.OnNext(1);
            subj.OnNext(2);
            subj.OnError(new InvalidOperationException());

            to.AssertFailure(typeof(InvalidOperationException));

            ts.AdvanceTimeBy(1000);

            to.AssertFailure(typeof(InvalidOperationException));
        }


        [Test]
        public void Time_Step_Error_Delayed()
        {
            var ts = new TestScheduler();

            var subj = new PublishSubject<int>();

            var to = subj.Delay(v => ObservableSource.Timer(TimeSpan.FromSeconds(1), ts), true).Test();

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
