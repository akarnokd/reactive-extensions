using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Collections.Generic;

namespace akarnokd.reactive_extensions_test.maybe
{
    [TestFixture]
    public class MaybeConcatEagerTest
    {
        #region + Array +

        #region + Max +

        [Test]
        public void Array_Max_Null()
        {
            new IMaybeSource<int>[]
            {
                MaybeSource.Just(1),
                MaybeSource.Empty<int>(),
                null
            }
            .ConcatEagerAll()
            .Test()
            .AssertFailure(typeof(NullReferenceException), 1);
        }

        [Test]
        public void Array_Max_Empty()
        {
            new IMaybeSource<int>[]
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
                MaybeSource.Just(1),
                MaybeSource.Just(2),
                MaybeSource.Empty<int>(),
                MaybeSource.Just(3)
            }
            .ConcatEagerAll()
            .Test()
            .AssertResult(1, 2, 3);
        }

        [Test]
        public void Array_Max_Basic_All_Empty()
        {
            MaybeSource.ConcatEager(
                MaybeSource.Empty<int>(),
                MaybeSource.Empty<int>(),
                MaybeSource.Empty<int>(),
                MaybeSource.Empty<int>()
            )
            .Test()
            .AssertResult();
        }

        [Test]
        public void Array_Max_Error()
        {
            new[]
            {
                MaybeSource.Just(1),
                MaybeSource.Empty<int>(),
                MaybeSource.Error<int>(new InvalidOperationException())
            }
            .ConcatEagerAll()
            .Test()
            .AssertFailure(typeof(InvalidOperationException), 1);
        }

        [Test]
        public void Array_Max_Error_Stop()
        {
            var count = 0;

            var src = MaybeSource.FromFunc(() => ++count);

            new[]
            {
                MaybeSource.Just(1),
                MaybeSource.Error<int>(new InvalidOperationException()),
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

            var src = MaybeSource.FromFunc(() => ++count);

            MaybeSource.ConcatEager(true,
                MaybeSource.Just(0),
                MaybeSource.Error<int>(new InvalidOperationException()),
                src
            )
            .Test()
            .AssertFailure(typeof(InvalidOperationException), 0, 1);

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Array_Max_Dispose()
        {
            var ms1 = new MaybeSubject<int>();
            var ms2 = new MaybeSubject<int>();

            var to = MaybeSource.ConcatEager(ms1, ms2)
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
            var ms1 = new MaybeSubject<int>();
            var ms2 = new MaybeSubject<int>();

            var to = MaybeSource.ConcatEager(ms1, ms2)
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
            var ms1 = new MaybeSubject<int>();
            var ms2 = new MaybeSubject<int>();

            var to = MaybeSource.ConcatEager(ms1, ms2)
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
            var ms1 = new MaybeSubject<int>();
            var ms2 = new MaybeSubject<int>();

            var to = MaybeSource.ConcatEager(
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
                var to = new IMaybeSource<int>[]
                {
                    MaybeSource.Just(1),
                    MaybeSource.Empty<int>(),
                    null
                }
                .ConcatEagerAll(maxConcurrency: i)
                .Test()
                .WithTag($"maxConcurrency={i}");

                if (i >= 3)
                {
                    // error cuts ahead
                    to.AssertFailure(typeof(NullReferenceException));
                }
                else
                {
                    to.AssertFailure(typeof(NullReferenceException), 1);
                }
            }
        }

        [Test]
        public void Array_Limit_Empty()
        {
            for (int i = 1; i < 10; i++)
            {
                MaybeSource.ConcatEager<int>(maxConcurrency: i)
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
                MaybeSource.ConcatEager<int>(i,
                    MaybeSource.Just(1),
                    MaybeSource.Just(2),
                    MaybeSource.Empty<int>(),
                    MaybeSource.Just(3)
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
                MaybeSource.ConcatEager<int>(true, i,
                    MaybeSource.Just(1),
                    MaybeSource.Just(2),
                    MaybeSource.Empty<int>(),
                    MaybeSource.Just(3)
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
                    MaybeSource.Just(1),
                    MaybeSource.Empty<int>(),
                    MaybeSource.Error<int>(new InvalidOperationException())
                }
                .ConcatEagerAll(maxConcurrency: i)
                .Test()
                .WithTag($"maxConcurrency={i}");

                if (i >= 3)
                { 
                    // error cuts ahead
                    to.AssertFailure(typeof(InvalidOperationException));
                }
                else
                {
                    to.AssertFailure(typeof(InvalidOperationException), 1);
                }
            }
        }

        [Test]
        public void Array_Limit_Error_Stop()
        {
            for (int i = 1; i < 10; i++)
            {
                var count = 0;

                var src = MaybeSource.FromFunc(() => ++count);

                var to = new[]
                {
                    MaybeSource.Just(1),
                    MaybeSource.Error<int>(new InvalidOperationException()),
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

                var src = MaybeSource.FromFunc(() => ++count);

                MaybeSource.ConcatEager(true, i,
                    MaybeSource.Just(0),
                    MaybeSource.Error<int>(new InvalidOperationException()),
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
            var ms1 = new MaybeSubject<int>();
            var ms2 = new MaybeSubject<int>();

            var to = MaybeSource.ConcatEager(1,
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
            var ms1 = new MaybeSubject<int>();
            var ms2 = new MaybeSubject<int>();

            var to = MaybeSource.ConcatEager(2,
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
            new List<IMaybeSource<int>>()
            {
                MaybeSource.Just(1),
                MaybeSource.Empty<int>(),
                null
            }
            .ConcatEager()
            .Test()
            .AssertFailure(typeof(NullReferenceException), 1);
        }

        [Test]
        public void Enumerable_Max_Empty()
        {
            new List<IMaybeSource<int>>()
            {

            }
            .ConcatEager()
            .Test()
            .AssertResult();
        }

        [Test]
        public void Enumerable_Max_Basic()
        {
            new List<IMaybeSource<int>>()
            {
                MaybeSource.Just(1),
                MaybeSource.Just(2),
                MaybeSource.Empty<int>(),
                MaybeSource.Just(3)
            }
            .ConcatEager()
            .Test()
            .AssertResult(1, 2, 3);
        }

        [Test]
        public void Enumerable_Max_Basic_All_Empty()
        {
            MaybeSource.ConcatEager(
                new List<IMaybeSource<int>>() {
                    MaybeSource.Empty<int>(),
                    MaybeSource.Empty<int>(),
                    MaybeSource.Empty<int>(),
                    MaybeSource.Empty<int>()
                }
            )
            .Test()
            .AssertResult();
        }

        [Test]
        public void Enumerable_Max_Error()
        {
            new List<IMaybeSource<int>>()
            {
                MaybeSource.Just(1),
                MaybeSource.Empty<int>(),
                MaybeSource.Error<int>(new InvalidOperationException())
            }
            .ConcatEager()
            .Test()
            .AssertFailure(typeof(InvalidOperationException), 1);
        }

        [Test]
        public void Enumerable_Max_Error_Stop()
        {
            var count = 0;

            var src = MaybeSource.FromFunc(() => ++count);

            new List<IMaybeSource<int>>()
            {
                MaybeSource.Just(1),
                MaybeSource.Error<int>(new InvalidOperationException()),
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

            var src = MaybeSource.FromFunc(() => ++count);

            MaybeSource.ConcatEager(
                new List<IMaybeSource<int>>() {
                    MaybeSource.Just(0),
                    MaybeSource.Error<int>(new InvalidOperationException()),
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
            var ms1 = new MaybeSubject<int>();
            var ms2 = new MaybeSubject<int>();

            var to = MaybeSource.ConcatEager(
                new List<IMaybeSource<int>>() {
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
            var ms1 = new MaybeSubject<int>();
            var ms2 = new MaybeSubject<int>();

            var to = MaybeSource.ConcatEager(
                new List<IMaybeSource<int>>() {
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
            var ms1 = new MaybeSubject<int>();
            var ms2 = new MaybeSubject<int>();

            var to = MaybeSource.ConcatEager(
                new List<IMaybeSource<int>>() {
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
            var ms1 = new MaybeSubject<int>();
            var ms2 = new MaybeSubject<int>();

            var to = MaybeSource.ConcatEager(
                new List<IMaybeSource<int>>() {
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
            MaybeSource.ConcatEager(new FailingEnumerable<IMaybeSource<int>>(true, false, false))
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Enumerable_Max_GetEnumerator_Crash_DelayErrors()
        {
            MaybeSource.ConcatEager(new FailingEnumerable<IMaybeSource<int>>(true, false, false), true)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Enumerable_Max_MoveNext_Crash()
        {
            MaybeSource.ConcatEager(new FailingEnumerable<IMaybeSource<int>>(false, true, false))
            .Test()
            .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Enumerable_Max_MoveNext_Crash_DelayErrors()
        {
            MaybeSource.ConcatEager(new FailingEnumerable<IMaybeSource<int>>(false, true, false), true)
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
                var to = new List<IMaybeSource<int>>()
                {
                    MaybeSource.Just(1),
                    MaybeSource.Empty<int>(),
                    null
                }
                .ConcatEager(maxConcurrency: i)
                .Test()
                .WithTag($"maxConcurrency={i}");

                if (i >= 3)
                {
                    // error cuts ahead
                    to.AssertFailure(typeof(NullReferenceException));
                }
                else
                {
                    to.AssertFailure(typeof(NullReferenceException), 1);
                }
            }
        }

        [Test]
        public void Enumerable_Limit_Empty()
        {
            for (int i = 1; i < 10; i++)
            {
                MaybeSource.ConcatEager<int>(maxConcurrency: i)
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
                MaybeSource.ConcatEager<int>(
                    new List<IMaybeSource<int>>() {
                        MaybeSource.Just(1),
                        MaybeSource.Just(2),
                        MaybeSource.Empty<int>(),
                        MaybeSource.Just(3)
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
                MaybeSource.ConcatEager<int>(
                    new List<IMaybeSource<int>>() {
                        MaybeSource.Just(1),
                        MaybeSource.Just(2),
                        MaybeSource.Empty<int>(),
                        MaybeSource.Just(3)
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
                var to = new List<IMaybeSource<int>>()
                {
                    MaybeSource.Just(1),
                    MaybeSource.Empty<int>(),
                    MaybeSource.Error<int>(new InvalidOperationException())
                }
                .ConcatEager(maxConcurrency: i)
                .Test()
                .WithTag($"maxConcurrency={i}");

                if (i >= 3)
                {
                    // error cuts ahead
                    to.AssertFailure(typeof(InvalidOperationException));
                }
                else
                {
                    to.AssertFailure(typeof(InvalidOperationException), 1);
                }
            }
        }

        [Test]
        public void Enumerable_Limit_Error_Stop()
        {
            for (int i = 1; i < 10; i++)
            {
                var count = 0;

                var src = MaybeSource.FromFunc(() => ++count);

                var to = new List<IMaybeSource<int>>()
                {
                    MaybeSource.Just(1),
                    MaybeSource.Error<int>(new InvalidOperationException()),
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

                var src = MaybeSource.FromFunc(() => ++count);

                MaybeSource.ConcatEager(
                    new List<IMaybeSource<int>>() {
                        MaybeSource.Just(0),
                        MaybeSource.Error<int>(new InvalidOperationException()),
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
            var ms1 = new MaybeSubject<int>();
            var ms2 = new MaybeSubject<int>();

            var to = MaybeSource.ConcatEager(
                new List<IMaybeSource<int>>() { ms1, ms2 }
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
            var ms1 = new MaybeSubject<int>();
            var ms2 = new MaybeSubject<int>();

            var to = MaybeSource.ConcatEager(
                new List<IMaybeSource<int>>() {
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
                MaybeSource.ConcatEager(new FailingEnumerable<IMaybeSource<int>>(true, false, false), maxConcurrency: i)
                    .Test()
                    .AssertFailure(typeof(InvalidOperationException));
            }
        }

        [Test]
        public void Enumerable_Limit_GetEnumerator_Crash_DelayErrors()
        {
            for (int i = 1; i < 10; i++)
            {
                MaybeSource.ConcatEager(new FailingEnumerable<IMaybeSource<int>>(true, false, false), true, i)
                    .Test()
                    .AssertFailure(typeof(InvalidOperationException));
            }
        }

        [Test]
        public void Enumerable_Limit_MoveNext_Crash()
        {
            for (int i = 1; i < 10; i++)
            {
                MaybeSource.ConcatEager(new FailingEnumerable<IMaybeSource<int>>(false, true, false), maxConcurrency: i)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
            }
        }

        [Test]
        public void Enumerable_Limit_MoveNext_Crash_DelayErrors()
        {
            for (int i = 1; i < 10; i++)
            {
                MaybeSource.ConcatEager(new FailingEnumerable<IMaybeSource<int>>(false, true, false), true, i)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
            }
        }

        #endregion + Limit +

        #endregion + Enumerable +
    }
}
