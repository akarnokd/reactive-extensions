using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Collections.Generic;

namespace akarnokd.reactive_extensions_test.single
{
    [TestFixture]
    public class SingleConcatEagerTest
    {
        #region + Array +

        #region + Max +

        [Test]
        public void Array_Max_Null()
        {
            new ISingleSource<int>[]
            {
                SingleSource.Just(1),
                null
            }
            .ConcatEagerAll()
            .Test()
            .AssertFailure(typeof(NullReferenceException), 1);
        }

        [Test]
        public void Array_Max_Empty()
        {
            new ISingleSource<int>[]
            {

            }
            .ConcatEagerAll()
            .Test()
            .AssertResult();
        }

        [Test]
        public void Array_Max_Basic()
        {
            new []
            {
                SingleSource.Just(1),
                SingleSource.Just(2),
                SingleSource.Just(3)
            }
            .ConcatEagerAll()
            .Test()
            .AssertResult(1, 2, 3);
        }

        [Test]
        public void Array_Max_Error()
        {
            new[]
            {
                SingleSource.Just(1),
                SingleSource.Error<int>(new InvalidOperationException())
            }
            .ConcatEagerAll()
            .Test()
            .AssertFailure(typeof(InvalidOperationException), 1);
        }

        [Test]
        public void Array_Max_Error_Stop()
        {
            var count = 0;

            var src = SingleSource.FromFunc(() => ++count);

            new[]
            {
                SingleSource.Just(1),
                SingleSource.Error<int>(new InvalidOperationException()),
                src
            }
            .ConcatEagerAll()
            .Test()
            .AssertFailure(typeof(InvalidOperationException), 1);

            Assert.AreEqual(0, count);
        }

        [Test]
        public void Array_Max_Error_Delay()
        {
            var count = 0;

            var src = SingleSource.FromFunc(() => ++count);

            SingleSource.ConcatEager(true,
                SingleSource.Just(0),
                SingleSource.Error<int>(new InvalidOperationException()),
                src
            )
            .Test()
            .AssertFailure(typeof(InvalidOperationException), 0, 1);

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Array_Max_Dispose()
        {
            var ms1 = new SingleSubject<int>();
            var ms2 = new SingleSubject<int>();

            var to = SingleSource.ConcatEager(ms1, ms2)
                .Test();

            Assert.True(ms1.HasObserver());
            Assert.True(ms2.HasObserver());

            to.Dispose();

            Assert.False(ms1.HasObserver());
            Assert.False(ms2.HasObserver());
        }

        [Test]
        public void Array_Max_Error_Dispose_First()
        {
            var ms1 = new SingleSubject<int>();
            var ms2 = new SingleSubject<int>();

            var to = SingleSource.ConcatEager(ms1, ms2)
                .Test();

            Assert.True(ms1.HasObserver());
            Assert.True(ms2.HasObserver());

            ms1.OnError(new InvalidOperationException());

            Assert.False(ms1.HasObserver());
            Assert.False(ms2.HasObserver());

            to.AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Array_Max_Error_Dispose_Second()
        {
            var ms1 = new SingleSubject<int>();
            var ms2 = new SingleSubject<int>();

            var to = SingleSource.ConcatEager(ms1, ms2)
                .Test();

            Assert.True(ms1.HasObserver());
            Assert.True(ms2.HasObserver());

            ms2.OnError(new InvalidOperationException());

            Assert.False(ms1.HasObserver());
            Assert.False(ms2.HasObserver());

            to.AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Array_Max_Keep_Order()
        {
            var ms1 = new SingleSubject<int>();
            var ms2 = new SingleSubject<int>();

            var to = SingleSource.ConcatEager(
                ms1, ms2
            )
            .Test();

            Assert.True(ms1.HasObserver());
            Assert.True(ms2.HasObserver());

            ms2.OnSuccess(2);

            to.AssertEmpty();

            ms1.OnSuccess(1);

            to.AssertResult(1, 2);
        }

        #endregion + Max +

        #region + Limit +

        [Test]
        public void Array_Limit_Null()
        {
            for (int i = 1; i < 10; i++)
            {
                var to = new ISingleSource<int>[]
                {
                    SingleSource.Just(1),
                    null
                }
                .ConcatEagerAll(maxConcurrency: i)
                .Test()
                .WithTag($"maxConcurrency={i}");

                to
                    .AssertNotCompleted()
                    .AssertError(typeof(NullReferenceException));
            }
        }

        [Test]
        public void Array_Limit_Empty()
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
        public void Array_Limit_Basic()
        {
            for (int i = 1; i < 10; i++)
            {
                SingleSource.ConcatEager<int>(i,
                    SingleSource.Just(1),
                    SingleSource.Just(2),
                    SingleSource.Just(3)
                    )
                    .Test()
                    .WithTag($"{i}")
                    .AssertResult(1, 2, 3);
            }
        }

        [Test]
        public void Array_Limit_Basic_Delay()
        {
            for (int i = 1; i < 10; i++)
            {
                SingleSource.ConcatEager<int>(true, i,
                    SingleSource.Just(1),
                    SingleSource.Just(2),
                    SingleSource.Just(3)
                    )
                    .Test()
                    .WithTag($"{i}")
                    .AssertResult(1, 2, 3);
            }
        }

        [Test]
        public void Array_Limit_Error()
        {
            for (int i = 1; i < 10; i++)
            {
                var to = new[]
                {
                    SingleSource.Just(1),
                    SingleSource.Error<int>(new InvalidOperationException())
                }
                .ConcatEagerAll(maxConcurrency: i)
                .Test()
                .WithTag($"maxConcurrency={i}");

                to
                    .AssertNotCompleted()
                    .AssertError(typeof(InvalidOperationException));
            }
        }

        [Test]
        public void Array_Limit_Error_Stop()
        {
            for (int i = 1; i < 10; i++)
            {
                var count = 0;

                var src = SingleSource.FromFunc(() => ++count);

                var to = new[]
                {
                    SingleSource.Just(1),
                    SingleSource.Error<int>(new InvalidOperationException()),
                    src
                }
                .ConcatEagerAll(maxConcurrency: i)
                .Test();

                if (i >= 2)
                { 
                    to.AssertFailure(typeof(InvalidOperationException));
                }
                else
                {
                    to.AssertFailure(typeof(InvalidOperationException), 1);
                }

                Assert.AreEqual(0, count);
            }
        }

        [Test]
        public void Array_Limit_Error_Delay()
        {
            for (int i = 1; i < 10; i++)
            {
                var count = 0;

                var src = SingleSource.FromFunc(() => ++count);

                SingleSource.ConcatEager(true, i,
                    SingleSource.Just(0),
                    SingleSource.Error<int>(new InvalidOperationException()),
                    src
                )
                .Test()
                .AssertFailure(typeof(InvalidOperationException), 0, 1);

                Assert.AreEqual(1, count);
            }
        }

        [Test]
        public void Array_Limit_Max_Concurrency()
        {
            var ms1 = new SingleSubject<int>();
            var ms2 = new SingleSubject<int>();

            var to = SingleSource.ConcatEager(1,
                ms1, ms2
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
        public void Array_Limit_Keep_Order()
        {
            var ms1 = new SingleSubject<int>();
            var ms2 = new SingleSubject<int>();

            var to = SingleSource.ConcatEager(2,
                ms1, ms2
            )
            .Test();

            Assert.True(ms1.HasObserver());
            Assert.True(ms2.HasObserver());

            ms2.OnSuccess(2);

            to.AssertEmpty();

            ms1.OnSuccess(1);

            to.AssertResult(1, 2);
        }

        #endregion + Limit +

        #endregion + Array +

        #region + Enumerable +

        #region + Max +

        [Test]
        public void Enumerable_Max_Null()
        {
            new List<ISingleSource<int>>()
            {
                SingleSource.Just(1),
                null
            }
            .ConcatEager()
            .Test()
            .AssertFailure(typeof(NullReferenceException), 1);
        }

        [Test]
        public void Enumerable_Max_Empty()
        {
            new List<ISingleSource<int>>()
            {

            }
            .ConcatEager()
            .Test()
            .AssertResult();
        }

        [Test]
        public void Enumerable_Max_Basic()
        {
            new List<ISingleSource<int>>()
            {
                SingleSource.Just(1),
                SingleSource.Just(2),
                SingleSource.Just(3)
            }
            .ConcatEager()
            .Test()
            .AssertResult(1, 2, 3);
        }

        [Test]
        public void Enumerable_Max_Error()
        {
            new List<ISingleSource<int>>()
            {
                SingleSource.Just(1),
                SingleSource.Error<int>(new InvalidOperationException())
            }
            .ConcatEager()
            .Test()
            .AssertFailure(typeof(InvalidOperationException), 1);
        }

        [Test]
        public void Enumerable_Max_Error_Stop()
        {
            var count = 0;

            var src = SingleSource.FromFunc(() => ++count);

            new List<ISingleSource<int>>()
            {
                SingleSource.Just(1),
                SingleSource.Error<int>(new InvalidOperationException()),
                src
            }
            .ConcatEager()
            .Test()
            .AssertFailure(typeof(InvalidOperationException), 1);

            Assert.AreEqual(0, count);
        }

        [Test]
        public void Enumerable_Max_Error_Delay()
        {
            var count = 0;

            var src = SingleSource.FromFunc(() => ++count);

            SingleSource.ConcatEager(
                new List<ISingleSource<int>>() {
                    SingleSource.Just(0),
                    SingleSource.Error<int>(new InvalidOperationException()),
                    src
                }, true
            )
            .Test()
            .AssertFailure(typeof(InvalidOperationException), 0, 1);

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Enumerable_Max_Dispose()
        {
            var ms1 = new SingleSubject<int>();
            var ms2 = new SingleSubject<int>();

            var to = SingleSource.ConcatEager(
                new List<ISingleSource<int>>() {
                    ms1, ms2
                })
                .Test();

            Assert.True(ms1.HasObserver());
            Assert.True(ms2.HasObserver());

            to.Dispose();

            Assert.False(ms1.HasObserver());
            Assert.False(ms2.HasObserver());
        }

        [Test]
        public void Enumerable_Max_Error_Dispose_First()
        {
            var ms1 = new SingleSubject<int>();
            var ms2 = new SingleSubject<int>();

            var to = SingleSource.ConcatEager(
                new List<ISingleSource<int>>() {
                    ms1, ms2
                }
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
        public void Enumerable_Max_Error_Dispose_Second()
        {
            var ms1 = new SingleSubject<int>();
            var ms2 = new SingleSubject<int>();

            var to = SingleSource.ConcatEager(
                new List<ISingleSource<int>>() {
                    ms1, ms2
                }
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
        public void Enumerable_Max_Keep_Order()
        {
            var ms1 = new SingleSubject<int>();
            var ms2 = new SingleSubject<int>();

            var to = SingleSource.ConcatEager(
                new List<ISingleSource<int>>() {
                    ms1, ms2
                }
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
        public void Enumerable_Max_GetEnumerator_Crash()
        {
            SingleSource.ConcatEager(new FailingEnumerable<ISingleSource<int>>(true, false, false))
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Enumerable_Max_GetEnumerator_Crash_DelayErrors()
        {
            SingleSource.ConcatEager(new FailingEnumerable<ISingleSource<int>>(true, false, false), true)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Enumerable_Max_MoveNext_Crash()
        {
            SingleSource.ConcatEager(new FailingEnumerable<ISingleSource<int>>(false, true, false))
            .Test()
            .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Enumerable_Max_MoveNext_Crash_DelayErrors()
        {
            SingleSource.ConcatEager(new FailingEnumerable<ISingleSource<int>>(false, true, false), true)
            .Test()
            .AssertFailure(typeof(InvalidOperationException));
        }

        #endregion + Max +

        #region + Limit +

        [Test]
        public void Enumerable_Limit_Null()
        {
            for (int i = 1; i < 10; i++)
            {
                var to = new List<ISingleSource<int>>()
                {
                    SingleSource.Just(1),
                    null
                }
                .ConcatEager(maxConcurrency: i)
                .Test()
                .WithTag($"maxConcurrency={i}");

                to
                    .AssertNotCompleted()
                    .AssertError(typeof(NullReferenceException));

            }
        }

        [Test]
        public void Enumerable_Limit_Empty()
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
        public void Enumerable_Limit_Basic()
        {
            for (int i = 1; i < 10; i++)
            {
                SingleSource.ConcatEager<int>(
                    new List<ISingleSource<int>>() {
                        SingleSource.Just(1),
                        SingleSource.Just(2),
                        SingleSource.Just(3)
                    }, maxConcurrency: i
                    )
                    .Test()
                    .WithTag($"{i}")
                    .AssertResult(1, 2, 3);
            }
        }

        [Test]
        public void Enumerable_Limit_Basic_Delay()
        {
            for (int i = 1; i < 10; i++)
            {
                SingleSource.ConcatEager<int>(
                    new List<ISingleSource<int>>() {
                        SingleSource.Just(1),
                        SingleSource.Just(2),
                        SingleSource.Just(3)
                    }
                    , true, i)
                    .Test()
                    .WithTag($"{i}")
                    .AssertResult(1, 2, 3);
            }
        }

        [Test]
        public void Enumerable_Limit_Error()
        {
            for (int i = 1; i < 10; i++)
            {
                var to = new List<ISingleSource<int>>()
                {
                    SingleSource.Just(1),
                    SingleSource.Error<int>(new InvalidOperationException())
                }
                .ConcatEager(maxConcurrency: i)
                .Test()
                .WithTag($"maxConcurrency={i}");

                to
                    .AssertNotCompleted()
                    .AssertError(typeof(InvalidOperationException));
            }
        }

        [Test]
        public void Enumerable_Limit_Error_Stop()
        {
            for (int i = 1; i < 10; i++)
            {
                var count = 0;

                var src = SingleSource.FromFunc(() => ++count);

                var to = new List<ISingleSource<int>>()
                {
                    SingleSource.Just(1),
                    SingleSource.Error<int>(new InvalidOperationException()),
                    src
                }
                .ConcatEager(maxConcurrency: i)
                .Test();

                if (i >= 2)
                {
                    to.AssertFailure(typeof(InvalidOperationException));
                }
                else
                {
                    to.AssertFailure(typeof(InvalidOperationException), 1);
                }

                Assert.AreEqual(0, count);
            }
        }

        [Test]
        public void Enumerable_Limit_Error_Delay()
        {
            for (int i = 1; i < 10; i++)
            {
                var count = 0;

                var src = SingleSource.FromFunc(() => ++count);

                SingleSource.ConcatEager(
                    new List<ISingleSource<int>>() {
                        SingleSource.Just(0),
                        SingleSource.Error<int>(new InvalidOperationException()),
                        src
                    }, true, i
                )
                .Test()
                .AssertFailure(typeof(InvalidOperationException), 0, 1);

                Assert.AreEqual(1, count);
            }
        }

        [Test]
        public void Enumerable_Limit_Max_Concurrency()
        {
            var ms1 = new SingleSubject<int>();
            var ms2 = new SingleSubject<int>();

            var to = SingleSource.ConcatEager(
                new List<ISingleSource<int>>() { ms1, ms2 }
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
        public void Enumerable_Limit_Keep_Order()
        {
            var ms1 = new SingleSubject<int>();
            var ms2 = new SingleSubject<int>();

            var to = SingleSource.ConcatEager(
                new List<ISingleSource<int>>() {
                    ms1, ms2
                }, maxConcurrency: 2
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
        public void Enumerable_Limit_GetEnumerator_Crash()
        {
            for (int i = 1; i < 10; i++)
            {
                SingleSource.ConcatEager(new FailingEnumerable<ISingleSource<int>>(true, false, false), maxConcurrency: i)
                    .Test()
                    .AssertFailure(typeof(InvalidOperationException));
            }
        }

        [Test]
        public void Enumerable_Limit_GetEnumerator_Crash_DelayErrors()
        {
            for (int i = 1; i < 10; i++)
            {
                SingleSource.ConcatEager(new FailingEnumerable<ISingleSource<int>>(true, false, false), true, i)
                    .Test()
                    .AssertFailure(typeof(InvalidOperationException));
            }
        }

        [Test]
        public void Enumerable_Limit_MoveNext_Crash()
        {
            for (int i = 1; i < 10; i++)
            {
                SingleSource.ConcatEager(new FailingEnumerable<ISingleSource<int>>(false, true, false), maxConcurrency: i)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
            }
        }

        [Test]
        public void Enumerable_Limit_MoveNext_Crash_DelayErrors()
        {
            for (int i = 1; i < 10; i++)
            {
                SingleSource.ConcatEager(new FailingEnumerable<ISingleSource<int>>(false, true, false), true, i)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
            }
        }

        #endregion + Limit +

        #endregion + Enumerable +
    }
}
