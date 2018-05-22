using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace akarnokd.reactive_extensions_test.single
{
    [TestFixture]
    public class SingleSwitchMapTest
    {
        #region + Eager errors +

        [Test]
        public void Eager_Basic()
        {
            Observable.Range(1, 5)
                .SwitchMap(v => SingleSource.Just(v + 1))
                .Test()
                .AssertResult(2, 3, 4, 5, 6);
        }

        [Test]
        public void Eager_Main_Error()
        {
            Observable.Range(1, 5).ConcatError(new InvalidOperationException())
                .SwitchMap(v => SingleSource.Just(v + 1))
                .Test()
                .AssertFailure(typeof(InvalidOperationException), 2, 3, 4, 5, 6);
        }

        [Test]
        public void Eager_Inner_Error()
        {
            Observable.Range(1, 5)
                .SwitchMap(v => {
                    if (v == 3)
                    {
                        return SingleSource.Error<int>(new InvalidOperationException());
                    }
                    return SingleSource.Just(v + 1);
                })
                .Test()
                .AssertFailure(typeof(InvalidOperationException), 2, 3);
        }

        [Test]
        public void Eager_Mapper_Crash()
        {
            var subj = new Subject<int>();

            var to = subj
                .SwitchMap(v => {
                    if (v == 3)
                    {
                        throw new InvalidOperationException();
                    }
                    return SingleSource.Just(v + 1);
                })
                .Test();

            subj.OnNext(1);

            to.AssertValuesOnly(2);

            subj.OnNext(2);

            to.AssertValuesOnly(2, 3);

            subj.OnNext(3);

            Assert.False(subj.HasObservers);

            to
                .AssertFailure(typeof(InvalidOperationException), 2, 3);
        }

        [Test]
        public void Eager_Switch_Normal()
        {
            var subj = new Subject<SingleSubject<int>>();

            var ms1 = new SingleSubject<int>();
            var ms2 = new SingleSubject<int>();
            var ms3 = new SingleSubject<int>();

            var to = subj.SwitchMap(v => v).Test();

            to.AssertEmpty();

            subj.OnNext(ms1);

            Assert.True(ms1.HasObserver());

            ms1.OnSuccess(1);

            to.AssertValuesOnly(1);

            subj.OnNext(ms2);

            Assert.True(ms2.HasObserver());

            subj.OnNext(ms3);

            Assert.False(ms2.HasObserver());
            Assert.True(ms3.HasObserver());

            subj.OnCompleted();

            Assert.True(ms3.HasObserver());

            ms3.OnSuccess(3);

            to.AssertResult(1, 3);
        }

        #endregion + Eager errors +

        #region + Delayed errors +

        [Test]
        public void Delayed_Basic()
        {
            Observable.Range(1, 5)
                .SwitchMap(v => SingleSource.Just(v + 1), true)
                .Test()
                .AssertResult(2, 3, 4, 5, 6);
        }

        [Test]
        public void Delayed_Main_Error()
        {
            Observable.Range(1, 5).ConcatError(new InvalidOperationException())
                .SwitchMap(v => SingleSource.Just(v + 1), true)
                .Test()
                .AssertFailure(typeof(InvalidOperationException), 2, 3, 4, 5, 6);
        }

        [Test]
        public void Delayed_Inner_Error()
        {
            Observable.Range(1, 5)
                .SwitchMap(v => {
                    if (v == 3)
                    {
                        return SingleSource.Error<int>(new InvalidOperationException());
                    }
                    return SingleSource.Just(v + 1);
                }, true)
                .Test()
                .AssertFailure(typeof(InvalidOperationException), 2, 3, 5, 6);
        }

        [Test]
        public void Delayed_Mapper_Crash()
        {
            var subj = new Subject<int>();

            var to = subj
                .SwitchMap(v => {
                    if (v == 3)
                    {
                        throw new InvalidOperationException();
                    }
                    return SingleSource.Just(v + 1);
                }, true)
                .Test();

            subj.OnNext(1);

            to.AssertValuesOnly(2);

            subj.OnNext(2);

            to.AssertValuesOnly(2, 3);

            subj.OnNext(3);

            Assert.False(subj.HasObservers);

            to
                .AssertFailure(typeof(InvalidOperationException), 2, 3);
        }

        [Test]
        public void Delayed_Switch_Normal()
        {
            var subj = new Subject<SingleSubject<int>>();

            var ms1 = new SingleSubject<int>();
            var ms2 = new SingleSubject<int>();
            var ms3 = new SingleSubject<int>();

            var to = subj.SwitchMap(v => v, true).Test();

            to.AssertEmpty();

            subj.OnNext(ms1);

            Assert.True(ms1.HasObserver());

            ms1.OnSuccess(1);

            to.AssertValuesOnly(1);

            subj.OnNext(ms2);

            Assert.True(ms2.HasObserver());

            subj.OnNext(ms3);

            Assert.False(ms2.HasObserver());
            Assert.True(ms3.HasObserver());

            subj.OnCompleted();

            Assert.True(ms3.HasObserver());

            ms3.OnSuccess(3);

            to.AssertResult(1, 3);
        }

        [Test]
        public void Delayed_Switch_Error_Success()
        {
            var subj = new Subject<SingleSubject<int>>();

            var ms1 = new SingleSubject<int>();
            var ms2 = new SingleSubject<int>();
            var ms3 = new SingleSubject<int>();

            var to = subj.SwitchMap(v => v, true).Test();

            to.AssertEmpty();

            subj.OnNext(ms1);

            Assert.True(ms1.HasObserver());

            ms1.OnError(new InvalidOperationException());

            to.AssertEmpty();

            subj.OnNext(ms2);

            Assert.True(ms2.HasObserver());

            subj.OnNext(ms3);

            Assert.False(ms2.HasObserver());
            Assert.True(ms3.HasObserver());

            subj.OnCompleted();

            Assert.True(ms3.HasObserver());

            ms3.OnSuccess(1);

            to.AssertFailure(typeof(InvalidOperationException), 1);
        }

        [Test]
        public void Delayed_Switch_Main_Errors_Success()
        {
            var subj = new Subject<SingleSubject<int>>();

            var ms1 = new SingleSubject<int>();
            var ms2 = new SingleSubject<int>();
            var ms3 = new SingleSubject<int>();

            var to = subj.SwitchMap(v => v, true).Test();

            to.AssertEmpty();

            subj.OnNext(ms1);

            Assert.True(ms1.HasObserver());

            ms1.OnSuccess(1);

            to.AssertValuesOnly(1);

            subj.OnNext(ms2);

            Assert.True(ms2.HasObserver());

            subj.OnNext(ms3);

            Assert.False(ms2.HasObserver());
            Assert.True(ms3.HasObserver());

            subj.OnError(new InvalidOperationException());

            Assert.True(ms3.HasObserver());

            ms3.OnSuccess(3);

            to.AssertFailure(typeof(InvalidOperationException), 1, 3);
        }

        #endregion + Delayed errors +
    }
}
