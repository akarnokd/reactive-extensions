using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace akarnokd.reactive_extensions_test.maybe
{
    [TestFixture]
    public class MaybeConcatMapTest
    {
        [Test]
        public void Basic()
        {
            Observable.Range(1, 5)
                .ConcatMap(v => MaybeSource.Just(v + 1))
                .Test()
                .AssertResult(2, 3, 4, 5, 6);
        }

        [Test]
        public void Basic_Empty()
        {
            Observable.Range(1, 5)
                .ConcatMap(v => MaybeSource.Empty<int>())
                .Test()
                .AssertResult();
        }

        [Test]
        public void Basic_Main_Error()
        {
            Observable.Range(1, 5).ConcatError(new InvalidOperationException())
                .ConcatMap(v => MaybeSource.Just(v + 1))
                .Test()
                .AssertFailure(typeof(InvalidOperationException), 2, 3, 4, 5, 6);
        }

        [Test]
        public void Basic_Inner_Error()
        {
            Observable.Range(1, 5)
                .ConcatMap(v => {
                    if (v == 3)
                    {
                        return MaybeSource.Error<int>(new InvalidOperationException());
                    }
                    return MaybeSource.Just(v + 1);
                })
                .Test()
                .AssertFailure(typeof(InvalidOperationException), 2, 3);
        }

        [Test]
        public void Dispose()
        {
            var subj = new Subject<int>();
            var ms = new MaybeSubject<int>();

            var to = subj.ConcatMap(v => ms).Test();

            Assert.True(subj.HasObservers);
            Assert.False(ms.HasObserver());

            subj.OnNext(1);

            Assert.True(subj.HasObservers);
            Assert.True(ms.HasObserver());

            to.Dispose();

            Assert.False(subj.HasObservers);
            Assert.False(ms.HasObserver());
        }

        [Test]
        public void Inner_Error_Disposes_Main()
        {
            var subj = new Subject<int>();
            var ms = new MaybeSubject<int>();

            var to = subj.ConcatMap(v => ms).Test();

            Assert.True(subj.HasObservers);
            Assert.False(ms.HasObserver());

            subj.OnNext(1);

            Assert.True(subj.HasObservers);
            Assert.True(ms.HasObserver());

            ms.OnError(new InvalidOperationException());

            Assert.False(subj.HasObservers);
            Assert.False(ms.HasObserver());

            to.AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Basic_DelayErrors()
        {
            Observable.Range(1, 5)
                .ConcatMap(v => MaybeSource.Just(v + 1), true)
                .Test()
                .AssertResult(2, 3, 4, 5, 6);
        }

        [Test]
        public void Basic_Empty_DelayErrors()
        {
            Observable.Range(1, 5)
                .ConcatMap(v => MaybeSource.Empty<int>(), true)
                .Test()
                .AssertResult();
        }

        [Test]
        public void Basic_Main_Error_DelayErrors()
        {
            Observable.Range(1, 5).ConcatError(new InvalidOperationException())
                .ConcatMap(v => MaybeSource.Just(v + 1), true)
                .Test()
                .AssertFailure(typeof(InvalidOperationException), 2, 3, 4, 5, 6);
        }

        [Test]
        public void Basic_Inner_Error_DelayErrors()
        {
            Observable.Range(1, 5)
                .ConcatMap(v => {
                    if (v == 3)
                    {
                        return MaybeSource.Error<int>(new InvalidOperationException());
                    }
                    return MaybeSource.Just(v + 1);
                }, true)
                .Test()
                .AssertFailure(typeof(InvalidOperationException), 2, 3, 5, 6);
        }

        [Test]
        public void Inner_Error_Delayed()
        {
            var subj = new Subject<int>();
            var ms = new MaybeSubject<int>();

            var to = subj.ConcatMap(v => ms, true).Test();

            Assert.True(subj.HasObservers);
            Assert.False(ms.HasObserver());

            subj.OnNext(1);

            Assert.True(subj.HasObservers);
            Assert.True(ms.HasObserver());

            ms.OnError(new InvalidOperationException());

            Assert.True(subj.HasObservers);
            Assert.False(ms.HasObserver());

            to.AssertEmpty();

            subj.OnCompleted();

            to.AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Mapper_Crash()
        {
            var subj = new Subject<int>();

            var to = subj
                .ConcatMap(v => {
                    if (v == 3)
                    {
                        throw new InvalidOperationException();
                    }
                    return MaybeSource.Just(v + 1);
                })
                .Test();

            subj.OnNext(1);

            Assert.True(subj.HasObservers);

            subj.OnNext(2);

            Assert.True(subj.HasObservers);

            subj.OnNext(3);

            Assert.False(subj.HasObservers);

            to
                .AssertFailure(typeof(InvalidOperationException), 2, 3);
        }

        [Test]
        public void Mapper_Crash_DelayErrors()
        {
            var subj = new Subject<int>();

            var to = subj
                .ConcatMap(v => {
                    if (v == 3)
                    {
                        throw new InvalidOperationException();
                    }
                    return MaybeSource.Just(v + 1);
                }, true)
                .Test();

            subj.OnNext(1);

            Assert.True(subj.HasObservers);

            subj.OnNext(2);

            Assert.True(subj.HasObservers);

            subj.OnNext(3);

            Assert.False(subj.HasObservers);

            to
                .AssertFailure(typeof(InvalidOperationException), 2, 3);
        }
    }
}
