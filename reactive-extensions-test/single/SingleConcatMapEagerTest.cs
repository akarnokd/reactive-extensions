using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace akarnokd.reactive_extensions_test.single
{
    [TestFixture]
    public class SingleConcatMapEagerTest
    {

        #region + Max +

        [Test]
        public void Max_Null()
        {
            new []
            {
                SingleSource.Just(1),
                null
            }.ToObservable()
            .ConcatEager()
            .Test()
            .AssertFailure(typeof(NullReferenceException), 1);
        }

        [Test]
        public void Max_Empty()
        {
            new ISingleSource<int>[0]
            {

            }.ToObservable()
            .ConcatEager()
            .Test()
            .AssertResult();
        }

        [Test]
        public void Max_Basic()
        {
            new []
            {
                SingleSource.Just(1),
                SingleSource.Just(2),
                SingleSource.Just(3)
            }.ToObservable()
            .ConcatEager()
            .Test()
            .AssertResult(1, 2, 3);
        }

        [Test]
        public void Max_Error()
        {
            new []
            {
                SingleSource.Just(1),
                SingleSource.Error<int>(new InvalidOperationException())
            }.ToObservable()
            .ConcatEager()
            .Test()
            .AssertFailure(typeof(InvalidOperationException), 1);
        }

        [Test]
        public void Max_Error_Stop()
        {
            var count = 0;

            var src = SingleSource.FromFunc(() => ++count);

            new []
            {
                SingleSource.Just(1),
                SingleSource.Error<int>(new InvalidOperationException()),
                src
            }.ToObservable()
            .ConcatEager()
            .Test()
            .AssertFailure(typeof(InvalidOperationException), 1);

            Assert.AreEqual(0, count);
        }

        [Test]
        public void Max_Error_Delay()
        {
            var count = 0;

            var src = SingleSource.FromFunc(() => ++count);

            SingleSource.ConcatEager(
                new [] {
                    SingleSource.Just(0),
                    SingleSource.Error<int>(new InvalidOperationException()),
                    src
                }.ToObservable(), true
            )
            .Test()
            .AssertFailure(typeof(InvalidOperationException), 0, 1);

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Max_Dispose()
        {
            var ms1 = new SingleSubject<int>();
            var ms2 = new SingleSubject<int>();

            var to = SingleSource.ConcatEager(
                new [] {
                    ms1, ms2
                }.ToObservable())
                .Test();

            Assert.True(ms1.HasObserver());
            Assert.True(ms2.HasObserver());

            to.Dispose();

            Assert.False(ms1.HasObserver());
            Assert.False(ms2.HasObserver());
        }

        [Test]
        public void Max_Error_Dispose_First()
        {
            var ms1 = new SingleSubject<int>();
            var ms2 = new SingleSubject<int>();

            var to = SingleSource.ConcatEager(
                new [] {
                    ms1, ms2
                }.ToObservable()
            )
            .Test();

            Assert.True(ms1.HasObserver());
            Assert.True(ms2.HasObserver());

            ms1.OnError(new InvalidOperationException());

            Assert.False(ms1.HasObserver());
            Assert.False(ms2.HasObserver());

            to.AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Max_Error_Dispose_Second()
        {
            var ms1 = new SingleSubject<int>();
            var ms2 = new SingleSubject<int>();

            var to = SingleSource.ConcatEager(
                new [] {
                    ms1, ms2
                }.ToObservable()
                )
                .Test();

            Assert.True(ms1.HasObserver());
            Assert.True(ms2.HasObserver());

            ms2.OnError(new InvalidOperationException());

            Assert.False(ms1.HasObserver());
            Assert.False(ms2.HasObserver());

            to.AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Max_Keep_Order()
        {
            var ms1 = new SingleSubject<int>();
            var ms2 = new SingleSubject<int>();

            var to = SingleSource.ConcatEager(
                new [] {
                    ms1, ms2
                }.ToObservable()
            )
            .Test();

            Assert.True(ms1.HasObserver());
            Assert.True(ms2.HasObserver());

            ms2.OnSuccess(2);

            to.AssertEmpty();

            ms1.OnSuccess(1);

            to.AssertResult(1, 2);
        }

        [Test]
        public void Max_Main_Error()
        {
            Observable.Throw<int>(new InvalidOperationException())
                .ConcatMapEager(v => SingleSource.Just(1))
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Max_Main_Error_Delay_Error()
        {
            Observable.Range(1, 5).ConcatError(new InvalidOperationException())
                .ConcatMapEager(v => SingleSource.Just(v), true)
                .Test()
                .AssertFailure(typeof(InvalidOperationException), 1, 2, 3, 4, 5);
        }

        [Test]
        public void Max_Dispose_Main()
        {
            var s = new Subject<int>();

            var to = s.ConcatMapEager(v => SingleSource.Just(v))
                .Test();

            Assert.True(s.HasObservers);

            to.Dispose();

            Assert.False(s.HasObservers);
        }

        [Test]
        public void Max_Dispose_Main_DelayError()
        {
            var s = new Subject<int>();

            var to = s.ConcatMapEager(v => SingleSource.Just(v), true)
                .Test();

            Assert.True(s.HasObservers);

            to.Dispose();

            Assert.False(s.HasObservers);
        }

        [Test]
        public void Max_Mapper_Crash()
        {
            Observable.Range(1, 5)
                .ConcatMapEager<int, int>(v =>
                {
                    if (v == 3)
                    {
                        throw new InvalidOperationException();
                    }
                    return SingleSource.Just(v);
                })
                .Test()
                .AssertNotCompleted()
                .AssertError(typeof(InvalidOperationException));
        }

        [Test]
        public void Max_Mapper_Crash_Delayed()
        {
            Observable.Range(1, 5)
                .ConcatMapEager<int, int>(v =>
                {
                    if (v == 3)
                    {
                        throw new InvalidOperationException();
                    }
                    return SingleSource.Just(v);
                }, true)
                .Test()
                .AssertFailure(typeof(InvalidOperationException), 1, 2);
        }

        #endregion + Max +

        #region + Limit +

        [Test]
        public void Limit_Null()
        {
            for (int i = 1; i < 10; i++)
            {
                var to = new []
                {
                    SingleSource.Just(1),
                    null
                }.ToObservable()
                .ConcatEager(maxConcurrency: i)
                .Test()
                .WithTag($"maxConcurrency={i}");

                to
                    .AssertNotCompleted()
                    .AssertError(typeof(NullReferenceException));
            }
        }

        [Test]
        public void Limit_Empty()
        {
            for (int i = 1; i < 10; i++)
            {
                SingleSource.ConcatEager<int>(maxConcurrency: i)
                    .Test()
                    .WithTag($"{i}")
                    .AssertResult();
            }
        }

        [Test]
        public void Limit_Basic()
        {
            for (int i = 1; i < 10; i++)
            {
                SingleSource.ConcatEager<int>(
                    new [] {
                        SingleSource.Just(1),
                        SingleSource.Just(2),
                        SingleSource.Just(3)
                    }.ToObservable(), maxConcurrency: i
                    )
                    .Test()
                    .WithTag($"{i}")
                    .AssertResult(1, 2, 3);
            }
        }

        [Test]
        public void Limit_Basic_Delay()
        {
            for (int i = 1; i < 10; i++)
            {
                SingleSource.ConcatEager<int>(
                    new [] {
                        SingleSource.Just(1),
                        SingleSource.Just(2),
                        SingleSource.Just(3)
                    }.ToObservable()
                    , true, i)
                    .Test()
                    .WithTag($"{i}")
                    .AssertResult(1, 2, 3);
            }
        }

        [Test]
        public void Limit_Error()
        {
            for (int i = 1; i < 10; i++)
            {
                var to = new []
                {
                    SingleSource.Just(1),
                    SingleSource.Error<int>(new InvalidOperationException())
                }.ToObservable()
                .ConcatEager(maxConcurrency: i)
                .Test()
                .WithTag($"maxConcurrency={i}");

                to
                    .AssertNotCompleted()
                    .AssertError(typeof(InvalidOperationException));

            }
        }

        [Test]
        public void Limit_Error_Stop()
        {
            for (int i = 1; i < 10; i++)
            {
                var count = 0;

                var src = SingleSource.FromFunc(() => ++count);

                var to = new []
                {
                    SingleSource.Just(1),
                    SingleSource.Error<int>(new InvalidOperationException()),
                    src
                }.ToObservable()
                .ConcatEager(maxConcurrency: i)
                .Test();

                to
                    .AssertNotCompleted()
                    .AssertError(typeof(InvalidOperationException));


                Assert.AreEqual(0, count);
            }
        }

        [Test]
        public void Limit_Error_Delay()
        {
            for (int i = 1; i < 10; i++)
            {
                var count = 0;

                var src = SingleSource.FromFunc(() => ++count);

                SingleSource.ConcatEager(
                    new [] {
                        SingleSource.Just(0),
                        SingleSource.Error<int>(new InvalidOperationException()),
                        src
                    }.ToObservable(), true, i
                )
                .Test()
                .AssertFailure(typeof(InvalidOperationException), 0, 1);

                Assert.AreEqual(1, count);
            }
        }

        [Test]
        public void Limit_Max_Concurrency()
        {
            var ms1 = new SingleSubject<int>();
            var ms2 = new SingleSubject<int>();

            var to = SingleSource.ConcatEager(
                new [] { ms1, ms2 }.ToObservable()
                , maxConcurrency: 1
            )
            .Test();

            Assert.True(ms1.HasObserver());
            Assert.False(ms2.HasObserver());

            ms1.OnSuccess(1);

            Assert.False(ms1.HasObserver());
            Assert.True(ms2.HasObserver());

            ms2.OnSuccess(2);

            to.AssertResult(1, 2);
        }

        [Test]
        public void Limit_Keep_Order()
        {
            var ms1 = new SingleSubject<int>();
            var ms2 = new SingleSubject<int>();

            var to = SingleSource.ConcatEager(
                new [] {
                    ms1, ms2
                }.ToObservable(), maxConcurrency: 2
            )
            .Test();

            Assert.True(ms1.HasObserver());
            Assert.True(ms2.HasObserver());

            ms2.OnSuccess(2);

            to.AssertEmpty();

            ms1.OnSuccess(1);

            to.AssertResult(1, 2);
        }

        [Test]
        public void Limit_GetEnumerator_Crash()
        {
            for (int i = 1; i < 10; i++)
            {
                SingleSource.ConcatEager(new FailingEnumerable<ISingleSource<int>>(true, false, false), maxConcurrency: i)
                    .Test()
                    .AssertFailure(typeof(InvalidOperationException));
            }
        }

        [Test]
        public void Limit_GetEnumerator_Crash_DelayErrors()
        {
            for (int i = 1; i < 10; i++)
            {
                SingleSource.ConcatEager(new FailingEnumerable<ISingleSource<int>>(true, false, false), true, i)
                    .Test()
                    .AssertFailure(typeof(InvalidOperationException));
            }
        }

        [Test]
        public void Limit_MoveNext_Crash()
        {
            for (int i = 1; i < 10; i++)
            {
                SingleSource.ConcatEager(new FailingEnumerable<ISingleSource<int>>(false, true, false), maxConcurrency: i)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
            }
        }

        [Test]
        public void Limit_MoveNext_Crash_DelayErrors()
        {
            for (int i = 1; i < 10; i++)
            {
                SingleSource.ConcatEager(new FailingEnumerable<ISingleSource<int>>(false, true, false), true, i)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
            }
        }

        [Test]
        public void Limit_Main_Error()
        {
            for (int i = 1; i < 10; i++)
            {
                Observable.Throw<int>(new InvalidOperationException())
                .ConcatMapEager(v => SingleSource.Just(1), maxConcurrency: i)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
            }
        }

        [Test]
        public void Limit_Main_Error_Delay_Error()
        {
            for (int i = 1; i < 10; i++)
            {
                Observable.Range(1, 5).ConcatError(new InvalidOperationException())
                    .ConcatMapEager(v => SingleSource.Just(v), true, i)
                    .Test()
                    .AssertFailure(typeof(InvalidOperationException), 1, 2, 3, 4, 5);
            }
        }

        [Test]
        public void Limit_Dispose_Main()
        {
            var s = new Subject<int>();

            var to = s.ConcatMapEager(v => SingleSource.Just(v), maxConcurrency: 1)
                .Test();

            Assert.True(s.HasObservers);

            to.Dispose();

            Assert.False(s.HasObservers);
        }

        [Test]
        public void Limit_Dispose_Main_DelayError()
        {
            var s = new Subject<int>();

            var to = s.ConcatMapEager(v => SingleSource.Just(v), true, 1)
                .Test();

            Assert.True(s.HasObservers);

            to.Dispose();

            Assert.False(s.HasObservers);
        }

        [Test]
        public void Limit_Mapper_Crash()
        {
            for (int i = 1; i < 10; i++)
            {
                Observable.Range(1, 5)
                    .ConcatMapEager<int, int>(v =>
                    {
                        if (v == 3)
                        {
                            throw new InvalidOperationException();
                        }
                        return SingleSource.Just(v);
                    }, maxConcurrency: i)
                    .Test()
                    .AssertNotCompleted()
                    .AssertError(typeof(InvalidOperationException));
            }
        }

        [Test]
        public void Limit_Mapper_Crash_Delayed()
        {
            for (int i = 1; i < 10; i++)
            {
                Observable.Range(1, 5)
                .ConcatMapEager<int, int>(v =>
                {
                    if (v == 3)
                    {
                        throw new InvalidOperationException();
                    }
                    return SingleSource.Just(v);
                }, true, i)
                .Test()
                .AssertFailure(typeof(InvalidOperationException), 1, 2);
            }
        }

        #endregion + Limit +
    }
}
