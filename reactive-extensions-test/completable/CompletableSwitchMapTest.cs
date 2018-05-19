using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace akarnokd.reactive_extensions_test.completable
{
    [TestFixture]
    public class CompletableSwitchMapTest
    {
        [Test]
        public void Basic()
        {
            var count = 0;

            Observable.Range(1, 5)
                .SwitchMap(v => CompletableSource.FromAction(() => count++))
                .Test()
                .AssertResult();

            Assert.AreEqual(5, count);
        }

        [Test]
        public void Empty()
        {
            var count = 0;

            Observable.Empty<int>()
                .SwitchMap(v => CompletableSource.FromAction(() => count++))
                .Test()
                .AssertResult();

            Assert.AreEqual(0, count);
        }

        [Test]
        public void Error()
        {
            var count = 0;

            Observable.Range(1, 5).ConcatError(new InvalidOperationException())
                .SwitchMap(v => CompletableSource.FromAction(() => count++))
                .Test()
                .AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(5, count);
        }

        [Test]
        public void Error_Delayed()
        {
            var count = 0;

            Observable.Range(1, 5).ConcatError(new InvalidOperationException())
                .SwitchMap(v => CompletableSource.FromAction(() => count++), true)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(5, count);
        }

        [Test]
        public void Error_Inner_Delayed()
        {
            var count = 0;

            Observable.Range(1, 5)
                .SwitchMap(v => {
                    if (v == 3)
                    {
                        return CompletableSource.Error(new InvalidOperationException());
                    }
                    return CompletableSource.FromAction(() => count++);
                }, true)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(4, count);
        }

        [Test]
        public void Error_Inner()
        {
            Observable.Return(1)
                .SwitchMap(v => CompletableSource.Error(new InvalidOperationException()))
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Switch()
        {
            var s = new UnicastSubject<ICompletableSource>();

            var cs1 = new CompletableSubject();
            var cs2 = new CompletableSubject();
            var cs3 = new CompletableSubject();

            var to = s
                .SwitchMap(v => v)
                .Test();

            to.AssertEmpty();

            Assert.False(cs1.HasObserver());
            Assert.False(cs2.HasObserver());
            Assert.False(cs3.HasObserver());

            s.OnNext(cs1);

            Assert.True(cs1.HasObserver());
            Assert.False(cs2.HasObserver());
            Assert.False(cs3.HasObserver());

            cs1.OnCompleted();

            Assert.False(cs2.HasObserver());
            Assert.False(cs3.HasObserver());

            s.OnNext(cs2);

            Assert.True(cs2.HasObserver());
            Assert.False(cs3.HasObserver());

            s.OnNext(cs3);

            Assert.False(cs2.HasObserver());
            Assert.True(cs3.HasObserver());

            to.AssertEmpty();

            s.OnCompleted();

            to.AssertEmpty();

            cs3.OnCompleted();

            to.AssertResult();
        }

        [Test]
        public void Race()
        {
            for (int i = 0; i < TestHelper.RACE_LOOPS; i++) {
                var s = new Subject<ICompletableSource>();

                var cs1 = new CompletableSubject();

                var to = s
                    .SwitchMap(v => v)
                    .Test();

                TestHelper.Race(() =>
                {
                    cs1.OnCompleted();
                },
                () =>
                {
                    s.OnNext(CompletableSource.Empty());
                    s.OnCompleted();
                }
                );

                to.AssertResult();
            }
        }


        [Test]
        public void Race_Error()
        {
            var ex = new InvalidOperationException();

            for (int i = 0; i < TestHelper.RACE_LOOPS; i++)
            {
                var s = new Subject<ICompletableSource>();

                var cs1 = new CompletableSubject();


                var to = s
                    .SwitchMap(v => v)
                    .Test();

                TestHelper.Race(() =>
                {
                    cs1.OnCompleted();
                },
                () =>
                {
                    s.OnNext(CompletableSource.Error(ex));
                    s.OnCompleted();
                }
                );

                to.AssertFailure(typeof(InvalidOperationException));
            }
        }

    }
}
