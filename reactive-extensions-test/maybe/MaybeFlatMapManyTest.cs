using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace akarnokd.reactive_extensions_test.maybe
{
    [TestFixture]
    public class MaybeFlatMapManyTest
    {

        #region + Max Concurrency +

        #region + Eager Errors +

        [Test]
        public void Max_Eager_Basic()
        {
            Observable.Range(1, 5)
                .FlatMap(v => MaybeSource.Just(v + 1))
                .Test()
                .AssertResult(2, 3, 4, 5, 6);
        }

        [Test]
        public void Max_Eager_Empty()
        {
            Observable.Range(1, 5)
                .FlatMap(v => MaybeSource.Empty<int>())
                .Test()
                .AssertResult();
        }

        [Test]
        public void Max_Eager_Error()
        {
            Observable.Range(1, 5).ConcatError(new InvalidOperationException())
                .FlatMap(v => MaybeSource.Just(v + 1))
                .Test()
                .AssertFailure(typeof(InvalidOperationException), 2, 3, 4, 5, 6);
        }

        [Test]
        public void Max_Eager_Inner_Error()
        {
            Observable.Range(1, 5)
                .FlatMap(v => {
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
        public void Max_Eager_Dispose()
        {
            var subj = new Subject<int>();
            var ms = new MaybeSubject<int>();

            var to = subj.FlatMap(v => ms).Test();

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
        public void Max_Eager_Main_Completes_Inner_Succeeds()
        {
            var subj = new Subject<int>();
            var ms = new MaybeSubject<int>();

            var to = subj.FlatMap(v => ms).Test();

            Assert.True(subj.HasObservers);
            Assert.False(ms.HasObserver());

            subj.OnNext(1);
            subj.OnCompleted();

            Assert.True(ms.HasObserver());

            ms.OnSuccess(1);

            Assert.False(subj.HasObservers);
            Assert.False(ms.HasObserver());

            to.AssertResult(1);
        }

        [Test]
        public void Max_Eager_Main_Completes_Inner_Empty()
        {
            var subj = new Subject<int>();
            var ms = new MaybeSubject<int>();

            var to = subj.FlatMap(v => ms).Test();

            Assert.True(subj.HasObservers);
            Assert.False(ms.HasObserver());

            subj.OnNext(1);
            subj.OnCompleted();

            Assert.True(ms.HasObserver());

            ms.OnCompleted();

            Assert.False(subj.HasObservers);
            Assert.False(ms.HasObserver());

            to.AssertResult();
        }

        [Test]
        public void Max_Eager_Dispose_Inner_Error()
        {
            var subj = new Subject<int>();
            var ms = new MaybeSubject<int>();

            var to = subj.FlatMap(v => ms).Test();

            Assert.True(subj.HasObservers);
            Assert.False(ms.HasObserver());

            subj.OnNext(1);

            Assert.True(ms.HasObserver());

            ms.OnError(new InvalidOperationException());

            Assert.False(subj.HasObservers);
            Assert.False(ms.HasObserver());

            to.AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Max_Eager_Race_Success()
        {
            for (int i = 0; i < TestHelper.RACE_LOOPS; i++)
            {
                var ms1 = new MaybeSubject<int>();
                var ms2 = new MaybeSubject<int>();

                var to = new[] {
                        ms1, ms2
                    }
                    .ToObservable()
                    .FlatMap(v => v)
                    .Test();

                TestHelper.Race(() => {
                    ms1.OnSuccess(1);
                }, () => {
                    ms2.OnSuccess(2);
                });

                to.AssertValueCount(2)
                    .AssertNoError()
                    .AssertCompleted();
            }
        }

        [Test]
        public void Max_Eager_Race_Empty()
        {
            for (int i = 0; i < TestHelper.RACE_LOOPS; i++)
            {
                var ms1 = new MaybeSubject<int>();
                var ms2 = new MaybeSubject<int>();

                var to = new[] {
                        ms1, ms2
                    }
                    .ToObservable()
                    .FlatMap(v => v)
                    .Test();

                TestHelper.Race(() => {
                    ms1.OnCompleted();
                }, () => {
                    ms2.OnCompleted();
                });

                to.AssertResult();
            }
        }

        [Test]
        public void Max_Eager_Race_Success_Empty()
        {
            for (int i = 0; i < TestHelper.RACE_LOOPS; i++)
            {
                var ms1 = new MaybeSubject<int>();
                var ms2 = new MaybeSubject<int>();

                var to = new[] {
                        ms1, ms2
                    }
                    .ToObservable()
                    .FlatMap(v => v)
                    .Test();

                TestHelper.Race(() => {
                    ms1.OnSuccess(1);
                }, () => {
                    ms2.OnCompleted();
                });

                to.AssertResult(1);
            }
        }

        [Test]
        public void Max_Eager_Mapper_Crash()
        {
            var subj = new Subject<int>();

            var to = subj.FlatMap(v => throw new InvalidOperationException())
                .Test();

            subj.OnNext(1);

            to.AssertFailure(typeof(InvalidOperationException));

            Assert.False(subj.HasObservers);
        }

        [Test]
        public void Max_Eager_Race_Upstream_Success()
        {
            for (int i = 0; i < TestHelper.RACE_LOOPS; i++)
            {
                var ms1 = new MaybeSubject<int>();
                var ms2 = new MaybeSubject<int>();

                var subj = new Subject<MaybeSubject<int>>();

                var to = subj.FlatMap(v => v).Test();

                subj.OnNext(ms1);

                TestHelper.Race(() =>
                {
                    ms1.OnSuccess(1);
                }, () =>
                {
                    subj.OnNext(ms2);
                    ms2.OnSuccess(2);
                });

                subj.OnCompleted();

                to.AssertValueCount(2)
                    .AssertNoError()
                    .AssertCompleted();
            }
        }

        #endregion + Eager Errors +

        #region + Delayed Errors +

        [Test]
        public void Max_Delayed_Basic()
        {
            Observable.Range(1, 5)
                .FlatMap(v => MaybeSource.Just(v + 1), true)
                .Test()
                .AssertResult(2, 3, 4, 5, 6);
        }

        [Test]
        public void Max_Delayed_Empty()
        {
            Observable.Range(1, 5)
                .FlatMap(v => MaybeSource.Empty<int>(), true)
                .Test()
                .AssertResult();
        }

        [Test]
        public void Max_Delayed_Error()
        {
            Observable.Range(1, 5).ConcatError(new InvalidOperationException())
                .FlatMap(v => MaybeSource.Just(v + 1), true)
                .Test()
                .AssertFailure(typeof(InvalidOperationException), 2, 3, 4, 5, 6);
        }

        [Test]
        public void Max_Delayed_Inner_Error()
        {
            Observable.Range(1, 5)
                .FlatMap(v => {
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
        public void Max_Delayed_Dispose()
        {
            var subj = new Subject<int>();
            var ms = new MaybeSubject<int>();

            var to = subj.FlatMap(v => ms, true).Test();

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
        public void Max_Delayed_Main_Completes_Inner_Succeeds()
        {
            var subj = new Subject<int>();
            var ms = new MaybeSubject<int>();

            var to = subj.FlatMap(v => ms, true).Test();

            Assert.True(subj.HasObservers);
            Assert.False(ms.HasObserver());

            subj.OnNext(1);
            subj.OnCompleted();

            Assert.True(ms.HasObserver());

            ms.OnSuccess(1);

            Assert.False(subj.HasObservers);
            Assert.False(ms.HasObserver());

            to.AssertResult(1);
        }

        [Test]
        public void Max_Delayed_Main_Completes_Inner_Empty()
        {
            var subj = new Subject<int>();
            var ms = new MaybeSubject<int>();

            var to = subj.FlatMap(v => ms, true).Test();

            Assert.True(subj.HasObservers);
            Assert.False(ms.HasObserver());

            subj.OnNext(1);
            subj.OnCompleted();

            Assert.True(ms.HasObserver());

            ms.OnCompleted();

            Assert.False(subj.HasObservers);
            Assert.False(ms.HasObserver());

            to.AssertResult();
        }

        [Test]
        public void Max_Delayed_Dispose_Inner_Error()
        {
            var subj = new Subject<int>();
            var ms = new MaybeSubject<int>();

            var to = subj.FlatMap(v => ms, true).Test();

            Assert.True(subj.HasObservers);
            Assert.False(ms.HasObserver());

            subj.OnNext(1);

            Assert.True(ms.HasObserver());

            ms.OnError(new InvalidOperationException());

            Assert.True(subj.HasObservers);
            Assert.False(ms.HasObserver());

            subj.OnCompleted();

            to.AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Max_Delayed_Race_Success()
        {
            for (int i = 0; i < TestHelper.RACE_LOOPS; i++)
            {
                var ms1 = new MaybeSubject<int>();
                var ms2 = new MaybeSubject<int>();

                var to = new[] {
                        ms1, ms2
                    }
                    .ToObservable()
                    .FlatMap(v => v, true)
                    .Test();

                TestHelper.Race(() => {
                    ms1.OnSuccess(1);
                }, () => {
                    ms2.OnSuccess(2);
                });

                to.AssertValueCount(2)
                    .AssertNoError()
                    .AssertCompleted();
            }
        }

        [Test]
        public void Max_Delayed_Race_Empty()
        {
            for (int i = 0; i < TestHelper.RACE_LOOPS; i++)
            {
                var ms1 = new MaybeSubject<int>();
                var ms2 = new MaybeSubject<int>();

                var to = new[] {
                        ms1, ms2
                    }
                    .ToObservable()
                    .FlatMap(v => v, true)
                    .Test();

                TestHelper.Race(() => {
                    ms1.OnCompleted();
                }, () => {
                    ms2.OnCompleted();
                });

                to.AssertResult();
            }
        }

        [Test]
        public void Max_Delayed_Race_Success_Empty()
        {
            for (int i = 0; i < TestHelper.RACE_LOOPS; i++)
            {
                var ms1 = new MaybeSubject<int>();
                var ms2 = new MaybeSubject<int>();

                var to = new[] {
                        ms1, ms2
                    }
                    .ToObservable()
                    .FlatMap(v => v, true)
                    .Test();

                TestHelper.Race(() => {
                    ms1.OnSuccess(1);
                }, () => {
                    ms2.OnCompleted();
                });

                to.AssertResult(1);
            }
        }

        [Test]
        public void Max_Delayed_Race_Success_Error()
        {
            var ex = new InvalidOperationException();

            for (int i = 0; i < TestHelper.RACE_LOOPS; i++)
            {
                var ms1 = new MaybeSubject<int>();
                var ms2 = new MaybeSubject<int>();

                var to = new[] {
                        ms1, ms2
                    }
                    .ToObservable()
                    .FlatMap(v => v, true)
                    .Test();

                TestHelper.Race(() => {
                    ms1.OnSuccess(1);
                }, () => {
                    ms2.OnError(ex);
                });

                to.AssertFailure(typeof(InvalidOperationException), 1);
            }
        }

        [Test]
        public void Max_Delayed_Mapper_Crash()
        {
            var subj = new Subject<int>();

            var to = subj.FlatMap(v => throw new InvalidOperationException(), true)
                .Test();

            subj.OnNext(1);

            to.AssertFailure(typeof(InvalidOperationException));

            Assert.False(subj.HasObservers);
        }

        [Test]
        public void Max_Delayed_Race_Upstream_Success()
        {
            for (int i = 0; i < TestHelper.RACE_LOOPS; i++)
            {
                var ms1 = new MaybeSubject<int>();
                var ms2 = new MaybeSubject<int>();

                var subj = new Subject<MaybeSubject<int>>();

                var to = subj.FlatMap(v => v, true).Test();

                subj.OnNext(ms1);

                TestHelper.Race(() =>
                {
                    ms1.OnSuccess(1);
                }, () =>
                {
                    subj.OnNext(ms2);
                    ms2.OnSuccess(2);
                });

                subj.OnCompleted();

                to.AssertValueCount(2)
                    .AssertNoError()
                    .AssertCompleted();
            }
        }

        #endregion + Delayed Errors +

        #endregion + Max Concurrency +

        #region + Limited Concurrency +

        #region + Eager Errors +

        [Test]
        public void Limited_Eager_Basic()
        {
            for (int k = 1; k < 10; k++)
            {
                Observable.Range(1, 5)
                    .FlatMap(v => MaybeSource.Just(v + 1), maxConcurrency: k)
                    .Test()
                    .AssertResult(2, 3, 4, 5, 6);
            }
        }

        [Test]
        public void Limited_Eager_Empty()
        {
            for (int k = 1; k < 10; k++)
            {
                Observable.Range(1, 5)
                .FlatMap(v => MaybeSource.Empty<int>(), maxConcurrency: k)
                .Test()
                .AssertResult();
            }
        }

        [Test]
        public void Limited_Eager_Error()
        {
            for (int k = 1; k < 10; k++)
            {
                Observable.Range(1, 5).ConcatError(new InvalidOperationException())
                .FlatMap(v => MaybeSource.Just(v + 1), maxConcurrency: k)
                .Test()
                .AssertFailure(typeof(InvalidOperationException), 2, 3, 4, 5, 6);
            }
        }

        [Test]
        public void Limited_Eager_Inner_Error()
        {
            for (int k = 1; k < 10; k++)
            {
                Observable.Range(1, 5)
                .FlatMap(v =>
                {
                    if (v == 3)
                    {
                        return MaybeSource.Error<int>(new InvalidOperationException());
                    }
                    return MaybeSource.Just(v + 1);
                }, maxConcurrency: k)
                .Test()
                .AssertFailure(typeof(InvalidOperationException), 2, 3);
            }
        }

        [Test]
        public void Limited_Eager_Dispose()
        {
            for (int k = 1; k < 10; k++)
            {
                var subj = new Subject<int>();
                var ms = new MaybeSubject<int>();

                var to = subj.FlatMap(v => ms, maxConcurrency: k).Test();

                Assert.True(subj.HasObservers);
                Assert.False(ms.HasObserver());

                subj.OnNext(1);

                Assert.True(subj.HasObservers);
                Assert.True(ms.HasObserver());

                to.Dispose();

                Assert.False(subj.HasObservers);
                Assert.False(ms.HasObserver());
            }
        }

        [Test]
        public void Limited_Eager_Main_Completes_Inner_Succeeds()
        {
            for (int k = 1; k < 10; k++)
            {
                var subj = new Subject<int>();
                var ms = new MaybeSubject<int>();

                var to = subj.FlatMap(v => ms, maxConcurrency: k).Test();

                Assert.True(subj.HasObservers);
                Assert.False(ms.HasObserver());

                subj.OnNext(1);
                subj.OnCompleted();

                Assert.True(ms.HasObserver());

                ms.OnSuccess(1);

                Assert.False(subj.HasObservers);
                Assert.False(ms.HasObserver());

                to.AssertResult(1);
            }
        }

        [Test]
        public void Limited_Eager_Main_Completes_Inner_Empty()
        {
            for (int k = 1; k < 10; k++)
            {
                var subj = new Subject<int>();
                var ms = new MaybeSubject<int>();

                var to = subj.FlatMap(v => ms, maxConcurrency: k).Test();

                Assert.True(subj.HasObservers);
                Assert.False(ms.HasObserver());

                subj.OnNext(1);
                subj.OnCompleted();

                Assert.True(ms.HasObserver());

                ms.OnCompleted();

                Assert.False(subj.HasObservers);
                Assert.False(ms.HasObserver());

                to.AssertResult();
            }
        }

        [Test]
        public void Limited_Eager_Dispose_Inner_Error()
        {
            for (int k = 1; k < 10; k++)
            {
                var subj = new Subject<int>();
                var ms = new MaybeSubject<int>();

                var to = subj.FlatMap(v => ms, maxConcurrency: k).Test();

                Assert.True(subj.HasObservers);
                Assert.False(ms.HasObserver());

                subj.OnNext(1);

                Assert.True(ms.HasObserver());

                ms.OnError(new InvalidOperationException());

                Assert.False(subj.HasObservers);
                Assert.False(ms.HasObserver());

                to.AssertFailure(typeof(InvalidOperationException));
            }
        }

        [Test]
        public void Limited_Eager_Race_Success()
        {
            for (int k = 1; k < 10; k++)
            {
                for (int i = 0; i < TestHelper.RACE_LOOPS; i++)
                {
                    var ms1 = new MaybeSubject<int>();
                    var ms2 = new MaybeSubject<int>();

                    var to = new[] {
                        ms1, ms2
                    }
                        .ToObservable()
                        .FlatMap(v => v, maxConcurrency: k)
                        .Test();

                    TestHelper.Race(() =>
                    {
                        ms1.OnSuccess(1);
                    }, () =>
                    {
                        ms2.OnSuccess(2);
                    });

                    to.AssertValueCount(2)
                        .AssertNoError()
                        .AssertCompleted();
                }
            }
        }

        [Test]
        public void Limited_Eager_Race_Empty()
        {
            for (int k = 1; k < 10; k++)
            {
                for (int i = 0; i < TestHelper.RACE_LOOPS; i++)
                {
                    var ms1 = new MaybeSubject<int>();
                    var ms2 = new MaybeSubject<int>();

                    var to = new[] {
                        ms1, ms2
                    }
                        .ToObservable()
                        .FlatMap(v => v, maxConcurrency: k)
                        .Test();

                    TestHelper.Race(() =>
                    {
                        ms1.OnCompleted();
                    }, () =>
                    {
                        ms2.OnCompleted();
                    });

                    to.AssertResult();
                }
            }
        }

        [Test]
        public void Limited_Eager_Race_Success_Empty()
        {
            for (int k = 1; k < 10; k++)
            {
                for (int i = 0; i < TestHelper.RACE_LOOPS; i++)
                {
                    var ms1 = new MaybeSubject<int>();
                    var ms2 = new MaybeSubject<int>();

                    var to = new[] {
                        ms1, ms2
                    }
                        .ToObservable()
                        .FlatMap(v => v, maxConcurrency: k)
                        .Test();

                    TestHelper.Race(() =>
                    {
                        ms1.OnSuccess(1);
                    }, () =>
                    {
                        ms2.OnCompleted();
                    });

                    to.AssertResult(1);
                }
            }
        }

        [Test]
        public void Limited_Eager_Mapper_Crash()
        {
            for (int k = 1; k < 10; k++)
            {
                var subj = new Subject<int>();

                var to = subj.FlatMap(v => throw new InvalidOperationException(), maxConcurrency: k)
                    .Test();

                subj.OnNext(1);

                to.AssertFailure(typeof(InvalidOperationException));

                Assert.False(subj.HasObservers);
            }
        }

        [Test]
        public void Limited_Eager_Success_First()
        {
            var ms1 = new MaybeSubject<int>();
            var ms2 = new MaybeSubject<int>();

            var to = new[] { ms1, ms2 }.ToObservable()
                .FlatMap(v => v, maxConcurrency: 1)
                .Test();

            Assert.True(ms1.HasObserver());
            Assert.False(ms2.HasObserver());

            ms1.OnSuccess(1);

            Assert.True(ms2.HasObserver());

            ms2.OnSuccess(2);

            to.AssertResult(1, 2);
        }

        [Test]
        public void Limited_Eager_Empty_First()
        {
            var ms1 = new MaybeSubject<int>();
            var ms2 = new MaybeSubject<int>();

            var to = new[] { ms1, ms2 }.ToObservable()
                .FlatMap(v => v, maxConcurrency: 1)
                .Test();

            Assert.True(ms1.HasObserver());
            Assert.False(ms2.HasObserver());

            ms1.OnCompleted();

            Assert.True(ms2.HasObserver());

            ms2.OnSuccess(2);

            to.AssertResult(2);
        }

        [Test]
        public void Limited_Eager_Error_First()
        {
            var ms1 = new MaybeSubject<int>();
            var ms2 = new MaybeSubject<int>();

            var to = new[] { ms1, ms2 }.ToObservable()
                .FlatMap(v => v, maxConcurrency: 1)
                .Test();

            Assert.True(ms1.HasObserver());
            Assert.False(ms2.HasObserver());

            ms1.OnError(new InvalidOperationException());

            Assert.False(ms2.HasObserver());

            to.AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Limited_Eager_Race_Upstream_Success()
        {
            for (int k = 1; k < 10; k++)
            {
                for (int i = 0; i < TestHelper.RACE_LOOPS; i++)
                {
                    var ms1 = new MaybeSubject<int>();
                    var ms2 = new MaybeSubject<int>();

                    var subj = new Subject<MaybeSubject<int>>();

                    var to = subj.FlatMap(v => v, maxConcurrency: k).Test();

                    subj.OnNext(ms1);

                    TestHelper.Race(() =>
                    {
                        ms1.OnSuccess(1);
                    }, () =>
                    {
                        subj.OnNext(ms2);
                        ms2.OnSuccess(2);
                    });

                    subj.OnCompleted();

                    to.AssertValueCount(2)
                        .AssertNoError()
                        .AssertCompleted();
                }
            }
        }

        #endregion + Eager Errors +

        #region + Delayed Errors +

        [Test]
        public void Limited_Delayed_Basic()
        {
            for (int k = 1; k < 10; k++)
            {
                Observable.Range(1, 5)
                .FlatMap(v => MaybeSource.Just(v + 1), true, k)
                .Test()
                .AssertResult(2, 3, 4, 5, 6);
            }
        }

        [Test]
        public void Limited_Delayed_Empty()
        {
            for (int k = 1; k < 10; k++)
            {
                Observable.Range(1, 5)
                .FlatMap(v => MaybeSource.Empty<int>(), true, k)
                .Test()
                .AssertResult();
            }
        }

        [Test]
        public void Limited_Delayed_Error()
        {
            for (int k = 1; k < 10; k++)
            {
                Observable.Range(1, 5).ConcatError(new InvalidOperationException())
                .FlatMap(v => MaybeSource.Just(v + 1), true, k)
                .Test()
                .AssertFailure(typeof(InvalidOperationException), 2, 3, 4, 5, 6);
            }
        }

        [Test]
        public void Limited_Delayed_Inner_Error()
        {
            for (int k = 1; k < 10; k++)
            {
                Observable.Range(1, 5)
                .FlatMap(v =>
                {
                    if (v == 3)
                    {
                        return MaybeSource.Error<int>(new InvalidOperationException());
                    }
                    return MaybeSource.Just(v + 1);
                }, true, k)
                .Test()
                .AssertFailure(typeof(InvalidOperationException), 2, 3, 5, 6);
            }
        }

        [Test]
        public void Limited_Delayed_Dispose()
        {
            for (int k = 1; k < 10; k++)
            {
                var subj = new Subject<int>();
                var ms = new MaybeSubject<int>();

                var to = subj.FlatMap(v => ms, true, k).Test();

                Assert.True(subj.HasObservers);
                Assert.False(ms.HasObserver());

                subj.OnNext(1);

                Assert.True(subj.HasObservers);
                Assert.True(ms.HasObserver());

                to.Dispose();

                Assert.False(subj.HasObservers);
                Assert.False(ms.HasObserver());
            }
        }

        [Test]
        public void Limited_Delayed_Main_Completes_Inner_Succeeds()
        {
            for (int k = 1; k < 10; k++)
            {
                var subj = new Subject<int>();
                var ms = new MaybeSubject<int>();

                var to = subj.FlatMap(v => ms, true, k).Test();

                Assert.True(subj.HasObservers);
                Assert.False(ms.HasObserver());

                subj.OnNext(1);
                subj.OnCompleted();

                Assert.True(ms.HasObserver());

                ms.OnSuccess(1);

                Assert.False(subj.HasObservers);
                Assert.False(ms.HasObserver());

                to.AssertResult(1);
            }
        }

        [Test]
        public void Limited_Delayed_Main_Completes_Inner_Empty()
        {
            for (int k = 1; k < 10; k++)
            {
                var subj = new Subject<int>();
                var ms = new MaybeSubject<int>();

                var to = subj.FlatMap(v => ms, true, k).Test();

                Assert.True(subj.HasObservers);
                Assert.False(ms.HasObserver());

                subj.OnNext(1);
                subj.OnCompleted();

                Assert.True(ms.HasObserver());

                ms.OnCompleted();

                Assert.False(subj.HasObservers);
                Assert.False(ms.HasObserver());

                to.AssertResult();
            }
        }

        [Test]
        public void Limited_Delayed_Dispose_Inner_Error()
        {
            for (int k = 1; k < 10; k++)
            {
                var subj = new Subject<int>();
                var ms = new MaybeSubject<int>();

                var to = subj.FlatMap(v => ms, true, k).Test();

                Assert.True(subj.HasObservers);
                Assert.False(ms.HasObserver());

                subj.OnNext(1);

                Assert.True(ms.HasObserver());

                ms.OnError(new InvalidOperationException());

                Assert.True(subj.HasObservers);
                Assert.False(ms.HasObserver());

                subj.OnCompleted();

                to.AssertFailure(typeof(InvalidOperationException));
            }
        }

        [Test]
        public void Limited_Delayed_Race_Success()
        {
            for (int k = 1; k < 10; k++)
            {
                for (int i = 0; i < TestHelper.RACE_LOOPS; i++)
                {
                    var ms1 = new MaybeSubject<int>();
                    var ms2 = new MaybeSubject<int>();

                    var to = new[] {
                        ms1, ms2
                    }
                        .ToObservable()
                        .FlatMap(v => v, true, k)
                        .Test();

                    TestHelper.Race(() =>
                    {
                        ms1.OnSuccess(1);
                    }, () =>
                    {
                        ms2.OnSuccess(2);
                    });

                    to.AssertValueCount(2)
                        .AssertNoError()
                        .AssertCompleted();
                }
            }
        }

        [Test]
        public void Limited_Delayed_Race_Empty()
        {
            for (int k = 1; k < 10; k++)
            {
                for (int i = 0; i < TestHelper.RACE_LOOPS; i++)
                {
                    var ms1 = new MaybeSubject<int>();
                    var ms2 = new MaybeSubject<int>();

                    var to = new[] {
                        ms1, ms2
                    }
                        .ToObservable()
                        .FlatMap(v => v, true, k)
                        .Test();

                    TestHelper.Race(() =>
                    {
                        ms1.OnCompleted();
                    }, () =>
                    {
                        ms2.OnCompleted();
                    });

                    to.AssertResult();
                }
            }
        }

        [Test]
        public void Limited_Delayed_Race_Success_Empty()
        {
            for (int k = 1; k < 10; k++)
            {
                for (int i = 0; i < TestHelper.RACE_LOOPS; i++)
                {
                    var ms1 = new MaybeSubject<int>();
                    var ms2 = new MaybeSubject<int>();

                    var to = new[] {
                        ms1, ms2
                    }
                        .ToObservable()
                        .FlatMap(v => v, true, k)
                        .Test();

                    TestHelper.Race(() =>
                    {
                        ms1.OnSuccess(1);
                    }, () =>
                    {
                        ms2.OnCompleted();
                    });

                    to.AssertResult(1);
                }
            }
        }

        [Test]
        public void Limited_Delayed_Race_Success_Error()
        {
            for (int k = 1; k < 10; k++)
            {
                var ex = new InvalidOperationException();

                for (int i = 0; i < TestHelper.RACE_LOOPS; i++)
                {
                    var ms1 = new MaybeSubject<int>();
                    var ms2 = new MaybeSubject<int>();

                    var to = new[] {
                        ms1, ms2
                    }
                        .ToObservable()
                        .FlatMap(v => v, true, k)
                        .Test();

                    TestHelper.Race(() =>
                    {
                        ms1.OnSuccess(1);
                    }, () =>
                    {
                        ms2.OnError(ex);
                    });

                    to.AssertFailure(typeof(InvalidOperationException), 1);
                }
            }
        }

        [Test]
        public void Limited_Delayed_Mapper_Crash()
        {
            for (int k = 1; k < 10; k++)
            {
                var subj = new Subject<int>();

                var to = subj.FlatMap(v => throw new InvalidOperationException(), true, k)
                    .Test();

                subj.OnNext(1);

                to.AssertFailure(typeof(InvalidOperationException));

                Assert.False(subj.HasObservers);
            }
        }

        [Test]
        public void Limited_Delayed_Success_First()
        {
            var ms1 = new MaybeSubject<int>();
            var ms2 = new MaybeSubject<int>();

            var to = new[] { ms1, ms2 }.ToObservable()
                .FlatMap(v => v, true, maxConcurrency: 1)
                .Test();

            Assert.True(ms1.HasObserver());
            Assert.False(ms2.HasObserver());

            ms1.OnSuccess(1);

            Assert.True(ms2.HasObserver());

            ms2.OnSuccess(2);

            to.AssertResult(1, 2);
        }

        [Test]
        public void Limited_Delayed_Empty_First()
        {
            var ms1 = new MaybeSubject<int>();
            var ms2 = new MaybeSubject<int>();

            var to = new[] { ms1, ms2 }.ToObservable()
                .FlatMap(v => v, true, maxConcurrency: 1)
                .Test();

            Assert.True(ms1.HasObserver());
            Assert.False(ms2.HasObserver());

            ms1.OnCompleted();

            Assert.True(ms2.HasObserver());

            ms2.OnSuccess(2);

            to.AssertResult(2);
        }

        [Test]
        public void Limited_Delayed_Error_First()
        {
            var ms1 = new MaybeSubject<int>();
            var ms2 = new MaybeSubject<int>();

            var to = new[] { ms1, ms2 }.ToObservable()
                .FlatMap(v => v, true, maxConcurrency: 1)
                .Test();

            Assert.True(ms1.HasObserver());
            Assert.False(ms2.HasObserver());

            ms1.OnError(new InvalidOperationException());

            Assert.True(ms2.HasObserver());

            ms2.OnSuccess(2);

            to.AssertFailure(typeof(InvalidOperationException), 2);
        }

        [Test]
        public void Limited_Delayed_Race_Upstream_Success()
        {
            for (int k = 1; k < 10; k++)
            {
                for (int i = 0; i < TestHelper.RACE_LOOPS; i++)
                {
                    var ms1 = new MaybeSubject<int>();
                    var ms2 = new MaybeSubject<int>();

                    var subj = new Subject<MaybeSubject<int>>();

                    var to = subj.FlatMap(v => v, true, maxConcurrency: k).Test();

                    subj.OnNext(ms1);

                    TestHelper.Race(() =>
                    {
                        ms1.OnSuccess(1);
                    }, () =>
                    {
                        subj.OnNext(ms2);
                        ms2.OnSuccess(2);
                    });

                    subj.OnCompleted();

                    to.AssertValueCount(2)
                        .AssertNoError()
                        .AssertCompleted();
                }
            }
        }

        #endregion + Delayed Errors +

        #endregion + Limited Concurrency +
    }
}
