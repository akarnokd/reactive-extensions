using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Collections.Generic;

namespace akarnokd.reactive_extensions_test.maybe
{
    [TestFixture]
    public class MaybeMergeTest
    {
        #region + Array +

        #region + Max Concurrency +

        [Test]
        public void Array_Max_Basic()
        {
            new[]
            {
                MaybeSource.Just(1),
                MaybeSource.Just(2),
                MaybeSource.Empty<int>(),
                MaybeSource.Just(3),
                MaybeSource.Empty<int>(),
                MaybeSource.Empty<int>()
            }
            .MergeAll()
            .Test()
            .AssertResult(1, 2, 3);
        }

        [Test]
        public void Array_Max_Basic_DelayError()
        {
            new[]
            {
                MaybeSource.Just(1),
                MaybeSource.Just(2),
                MaybeSource.Empty<int>(),
                MaybeSource.Just(3),
                MaybeSource.Empty<int>(),
                MaybeSource.Empty<int>()
            }
            .MergeAll(true)
            .Test()
            .AssertResult(1, 2, 3);
        }

        [Test]
        public void Array_Max_Empty()
        {
            new IMaybeSource<int>[]
            {
            }
            .MergeAll()
            .Test()
            .AssertResult();
        }

        [Test]
        public void Array_Max_Empty_DelayError()
        {
            new IMaybeSource<int>[]
            {
            }
            .MergeAll(true)
            .Test()
            .AssertResult();
        }

        [Test]
        public void Array_Max_Error()
        {
            MaybeSource.Merge(
                MaybeSource.Error<int>(new InvalidOperationException()),
                MaybeSource.Just(1)
            )
            .Test()
            .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Array_Max_Error_Delayed()
        {
            MaybeSource.Merge(true,
                MaybeSource.Just(1),
                MaybeSource.Error<int>(new InvalidOperationException()),
                MaybeSource.Empty<int>(),
                MaybeSource.Just(2)
            )
            .Test()
            .AssertFailure(typeof(InvalidOperationException), 1, 2);
        }

        [Test]
        public void Array_Max_Null_Source()
        {
            MaybeSource.Merge(
                MaybeSource.Just(1),
                null,
                MaybeSource.Empty<int>(),
                MaybeSource.Just(2)
            )
            .Test()
            .AssertNotCompleted()
            .AssertError(typeof(NullReferenceException));
        }

        [Test]
        public void Array_Max_Dispose()
        {
            var ms1 = new MaybeSubject<int>();
            var ms2 = new MaybeSubject<int>();

            var to = new[]
            {
                ms1,
                ms2
            }
            .MergeAll()
            .Test();

            Assert.True(ms1.HasObserver());
            Assert.True(ms2.HasObserver());

            to.Dispose();

            Assert.False(ms1.HasObserver());
            Assert.False(ms2.HasObserver());
        }

        [Test]
        public void Array_Max_Dispose_On_Error()
        {
            var ms1 = new MaybeSubject<int>();
            var ms2 = new MaybeSubject<int>();

            var to = new[]
            {
                ms1,
                ms2
            }
            .MergeAll()
            .Test();

            Assert.True(ms1.HasObserver());
            Assert.True(ms2.HasObserver());

            ms2.OnError(new InvalidOperationException());

            Assert.False(ms1.HasObserver());
            Assert.False(ms2.HasObserver());

            to.AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Array_Max_Race_Complete()
        {
            for (int i = 0; i < TestHelper.RACE_LOOPS; i++)
            {
                var ms1 = new MaybeSubject<int>();
                var ms2 = new MaybeSubject<int>();

                var to = new[]
                {
                        ms1,
                        ms2
                    }
                .MergeAll()
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

        [Test]
        public void Array_Max_Race_Success()
        {
            for (int i = 0; i < TestHelper.RACE_LOOPS; i++)
            {
                var ms1 = new MaybeSubject<int>();
                var ms2 = new MaybeSubject<int>();

                var to = new[]
                {
                    ms1,
                    ms2
                }
                .MergeAll()
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

        [Test]
        public void Array_Max_Race_Empty_Success()
        {
            for (int i = 0; i < TestHelper.RACE_LOOPS; i++)
            {
                var ms1 = new MaybeSubject<int>();
                var ms2 = new MaybeSubject<int>();

                var to = new[]
                {
                    ms1,
                    ms2
                }
                .MergeAll()
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

        #endregion + Max Concurrency +

        #region + Limited Concurrency +

        [Test]
        public void Array_Limited_Basic()
        {
            for (int i = 1; i < 10; i++)
            {
                new[]
                {
                MaybeSource.Just(1),
                MaybeSource.Just(2),
                MaybeSource.Empty<int>(),
                MaybeSource.Just(3),
                MaybeSource.Empty<int>(),
                MaybeSource.Empty<int>()
            }
                .MergeAll(maxConcurrency: i)
                .Test()
                .AssertResult(1, 2, 3);
            }
        }

        [Test]
        public void Array_Limited_Basic_DelayError()
        {
            for (int i = 1; i < 10; i++)
            {
                new[]
                {
                    MaybeSource.Just(1),
                    MaybeSource.Just(2),
                    MaybeSource.Empty<int>(),
                    MaybeSource.Just(3),
                    MaybeSource.Empty<int>(),
                    MaybeSource.Empty<int>()
                }
                .MergeAll(true, i)
                .Test()
                .AssertResult(1, 2, 3);
            }
        }

        [Test]
        public void Array_Limited_Empty()
        {
            for (int i = 1; i < 10; i++)
            {

                new IMaybeSource<int>[]
                {
                }
                .MergeAll(maxConcurrency: i)
                .Test()
                .AssertResult();
            }
        }

        [Test]
        public void Array_Limited_Empty_DelayError()
        {
            for (int i = 1; i < 10; i++)
            {
                new IMaybeSource<int>[]
                {
                }
                .MergeAll(true, i)
                .Test()
                .AssertResult();
            }
        }

        [Test]
        public void Array_Limited_Error()
        {
            for (int i = 1; i < 10; i++)
            {
                MaybeSource.Merge(i,
                    MaybeSource.Error<int>(new InvalidOperationException()),
                    MaybeSource.Just(1)
                )
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
            }
        }

        [Test]
        public void Array_Limited_Error_Delayed()
        {
            for (int i = 1; i < 10; i++)
            {
                MaybeSource.Merge(true, i,
                    MaybeSource.Just(1),
                    MaybeSource.Error<int>(new InvalidOperationException()),
                    MaybeSource.Empty<int>(),
                    MaybeSource.Just(2)
                )
                .Test()
                .AssertFailure(typeof(InvalidOperationException), 1, 2);
            }
        }

        [Test]
        public void Array_Limited_Null_Source()
        {
            for (int i = 1; i < 10; i++)
            {

                MaybeSource.Merge(i,
                    MaybeSource.Just(1),
                    null,
                    MaybeSource.Empty<int>(),
                    MaybeSource.Just(2)
                )
                .Test()
                .AssertNotCompleted()
                .AssertError(typeof(NullReferenceException));
            }
        }

        [Test]
        public void Array_Limited_Dispose()
        {
            var ms1 = new MaybeSubject<int>();
            var ms2 = new MaybeSubject<int>();

            var to = new[]
            {
                    ms1,
                    ms2
                }
            .MergeAll(maxConcurrency: 2)
            .Test();

            Assert.True(ms1.HasObserver());
            Assert.True(ms2.HasObserver());

            to.Dispose();

            Assert.False(ms1.HasObserver());
            Assert.False(ms2.HasObserver());
        }

        [Test]
        public void Array_Limited_Dispose_On_Error()
        {
            var ms1 = new MaybeSubject<int>();
            var ms2 = new MaybeSubject<int>();

            var to = new[]
            {
                ms1,
                ms2
            }
            .MergeAll(maxConcurrency: 2)
            .Test();

            Assert.True(ms1.HasObserver());
            Assert.True(ms2.HasObserver());

            ms2.OnError(new InvalidOperationException());

            Assert.False(ms1.HasObserver());
            Assert.False(ms2.HasObserver());

            to.AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Array_Limited_Subscription()
        {
            var ms1 = new MaybeSubject<int>();
            var ms2 = new MaybeSubject<int>();

            var to = new[]
            {
                ms1,
                ms2
            }
            .MergeAll(maxConcurrency: 1)
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
        public void Array_Limited_Subscription_Empty()
        {
            var ms1 = new MaybeSubject<int>();
            var ms2 = new MaybeSubject<int>();

            var to = new[]
            {
                ms1,
                ms2
            }
            .MergeAll(maxConcurrency: 1)
            .Test();

            Assert.True(ms1.HasObserver());
            Assert.False(ms2.HasObserver());

            ms1.OnCompleted();

            Assert.False(ms1.HasObserver());
            Assert.True(ms2.HasObserver());

            ms2.OnCompleted();

            to.AssertResult();
        }

        [Test]
        public void Array_Limited_Race_Complete()
        {
            for (int k = 1; k < 4; k++)
            {
                for (int i = 0; i < TestHelper.RACE_LOOPS; i++)
                {
                    var ms1 = new MaybeSubject<int>();
                    var ms2 = new MaybeSubject<int>();

                    var to = new[]
                    {
                        ms1,
                        ms2
                    }
                    .MergeAll(maxConcurrency: k)
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
        public void Array_Limited_Race_Success()
        {
            for (int k = 1; k < 4; k++)
            {
                for (int i = 0; i < TestHelper.RACE_LOOPS; i++)
                {
                    var ms1 = new MaybeSubject<int>();
                    var ms2 = new MaybeSubject<int>();

                    var to = new[]
                    {
                        ms1,
                        ms2
                    }
                    .MergeAll(maxConcurrency: k)
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
        public void Array_Limited_Race_Empty_Success()
        {
            for (int k = 1; k < 4; k++)
            {
                for (int i = 0; i < TestHelper.RACE_LOOPS; i++)
                {
                    var ms1 = new MaybeSubject<int>();
                    var ms2 = new MaybeSubject<int>();

                    var to = new[]
                    {
                        ms1,
                        ms2
                    }
                    .MergeAll(maxConcurrency: k)
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

        public void Enumerable_Max_GetEnumerator_Crash()
        {
            MaybeSource.Merge(new FailingEnumerable<IMaybeSource<int>>(true, false, false))
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        public void Enumerable_Max_GetEnumerator_Crash_DelayErrors()
        {
            MaybeSource.Merge(new FailingEnumerable<IMaybeSource<int>>(true, false, false), true)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        public void Enumerable_Max_MoveNext_Crash()
        {
            MaybeSource.Merge(new FailingEnumerable<IMaybeSource<int>>(false, true, false))
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        public void Enumerable_Max_MoveNext_Crash_DelayErrors()
        {
            MaybeSource.Merge(new FailingEnumerable<IMaybeSource<int>>(false, true, false), true)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        #endregion + Limited Concurrency +

        #endregion + Array +

        #region + Enumerable +

        [Test]
        public void Enumerable_Max_Basic()
        {
            new List<IMaybeSource<int>>()
            {
                MaybeSource.Just(1),
                MaybeSource.Just(2),
                MaybeSource.Empty<int>(),
                MaybeSource.Just(3),
                MaybeSource.Empty<int>(),
                MaybeSource.Empty<int>()
            }
            .Merge()
            .Test()
            .AssertResult(1, 2, 3);
        }

        [Test]
        public void Enumerable_Max_Basic_DelayError()
        {
            new List<IMaybeSource<int>>()
            {
                MaybeSource.Just(1),
                MaybeSource.Just(2),
                MaybeSource.Empty<int>(),
                MaybeSource.Just(3),
                MaybeSource.Empty<int>(),
                MaybeSource.Empty<int>()
            }
            .Merge(true)
            .Test()
            .AssertResult(1, 2, 3);
        }

        [Test]
        public void Enumerable_Max_Empty()
        {
            new List<IMaybeSource<int>>()
            {
            }
            .Merge()
            .Test()
            .AssertResult();
        }

        [Test]
        public void Enumerable_Max_Empty_DelayError()
        {
            new List<IMaybeSource<int>>()
            {
            }
            .Merge(true)
            .Test()
            .AssertResult();
        }

        [Test]
        public void Enumerable_Max_Error()
        {
            MaybeSource.Merge(
                new List<IMaybeSource<int>>()
                {
                    MaybeSource.Error<int>(new InvalidOperationException()),
                    MaybeSource.Just(1)
                }
            )
            .Test()
            .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Enumerable_Max_Error_Delayed()
        {
            MaybeSource.Merge(
                new List<IMaybeSource<int>>()
                {
                    MaybeSource.Just(1),
                    MaybeSource.Error<int>(new InvalidOperationException()),
                    MaybeSource.Empty<int>(),
                    MaybeSource.Just(2)
                }, true
            )
            .Test()
            .AssertFailure(typeof(InvalidOperationException), 1, 2);
        }

        [Test]
        public void Enumerable_Max_Null_Source()
        {
            MaybeSource.Merge(
                new List<IMaybeSource<int>>()
                {
                    MaybeSource.Just(1),
                    null,
                    MaybeSource.Empty<int>(),
                    MaybeSource.Just(2)
                }
            )
            .Test()
            .AssertNotCompleted()
            .AssertError(typeof(NullReferenceException));
        }

        [Test]
        public void Enumerable_Max_Dispose()
        {
            var ms1 = new MaybeSubject<int>();
            var ms2 = new MaybeSubject<int>();

            var to = new List<IMaybeSource<int>>()
            {
                ms1,
                ms2
            }
            .Merge()
            .Test();

            Assert.True(ms1.HasObserver());
            Assert.True(ms2.HasObserver());

            to.Dispose();

            Assert.False(ms1.HasObserver());
            Assert.False(ms2.HasObserver());
        }

        [Test]
        public void Enumerable_Max_Dispose_On_Error()
        {
            var ms1 = new MaybeSubject<int>();
            var ms2 = new MaybeSubject<int>();

            var to = new List<IMaybeSource<int>>()
            {
                ms1,
                ms2
            }
            .Merge()
            .Test();

            Assert.True(ms1.HasObserver());
            Assert.True(ms2.HasObserver());

            ms2.OnError(new InvalidOperationException());

            Assert.False(ms1.HasObserver());
            Assert.False(ms2.HasObserver());

            to.AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Enumerable_Max_Race_Complete()
        {
            for (int i = 0; i < TestHelper.RACE_LOOPS; i++)
            {
                var ms1 = new MaybeSubject<int>();
                var ms2 = new MaybeSubject<int>();

                var to = new List<IMaybeSource<int>>()
                {
                    ms1,
                    ms2
                }
                .Merge()
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

        [Test]
        public void Enumerable_Max_Race_Success()
        {
            for (int i = 0; i < TestHelper.RACE_LOOPS; i++)
            {
                var ms1 = new MaybeSubject<int>();
                var ms2 = new MaybeSubject<int>();

                var to = new List<IMaybeSource<int>>()
                {
                    ms1,
                    ms2
                }
                .Merge()
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

        [Test]
        public void Enumerable_Max_Race_Empty_Success()
        {
            for (int i = 0; i < TestHelper.RACE_LOOPS; i++)
            {
                var ms1 = new MaybeSubject<int>();
                var ms2 = new MaybeSubject<int>();

                var to = new List<IMaybeSource<int>>()
                {
                    ms1,
                    ms2
                }
                .Merge()
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

        #endregion + Max Concurrency +

        #region + Limited Concurrency +

        [Test]
        public void Enumerable_Limited_Basic()
        {
            for (int i = 1; i < 10; i++)
            {
                new List<IMaybeSource<int>>()
                {
                    MaybeSource.Just(1),
                    MaybeSource.Just(2),
                    MaybeSource.Empty<int>(),
                    MaybeSource.Just(3),
                    MaybeSource.Empty<int>(),
                    MaybeSource.Empty<int>()
                }
                .Merge(maxConcurrency: i)
                .Test()
                .AssertResult(1, 2, 3);
            }
        }

        [Test]
        public void Enumerable_Limited_Basic_DelayError()
        {
            for (int i = 1; i < 10; i++)
            {
                new List<IMaybeSource<int>>()
                {
                    MaybeSource.Just(1),
                    MaybeSource.Just(2),
                    MaybeSource.Empty<int>(),
                    MaybeSource.Just(3),
                    MaybeSource.Empty<int>(),
                    MaybeSource.Empty<int>()
                }
                .Merge(true, i)
                .Test()
                .AssertResult(1, 2, 3);
            }
        }

        [Test]
        public void Enumerable_Limited_Empty()
        {
            for (int i = 1; i < 10; i++)
            {

                new List<IMaybeSource<int>>()
                {
                }
                .Merge(maxConcurrency: i)
                .Test()
                .AssertResult();
            }
        }

        [Test]
        public void Enumerable_Limited_Empty_DelayError()
        {
            for (int i = 1; i < 10; i++)
            {
                new List<IMaybeSource<int>>()
                {
                }
                .Merge(true, i)
                .Test()
                .AssertResult();
            }
        }

        [Test]
        public void Enumerable_Limited_Error()
        {
            for (int i = 1; i < 10; i++)
            {
                MaybeSource.Merge(
                    new List<IMaybeSource<int>>() {
                        MaybeSource.Error<int>(new InvalidOperationException()),
                        MaybeSource.Just(1)
                    }, maxConcurrency: i
                )
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
            }
        }

        [Test]
        public void Enumerable_Limited_Error_Delayed()
        {
            for (int i = 1; i < 10; i++)
            {
                MaybeSource.Merge(
                    new List<IMaybeSource<int>>() {
                        MaybeSource.Just(1),
                        MaybeSource.Error<int>(new InvalidOperationException()),
                        MaybeSource.Empty<int>(),
                        MaybeSource.Just(2)
                    }, true, i
                )
                .Test()
                .AssertFailure(typeof(InvalidOperationException), 1, 2);
            }
        }

        [Test]
        public void Enumerable_Limited_Null_Source()
        {
            for (int i = 1; i < 10; i++)
            {

                MaybeSource.Merge(
                    new List<IMaybeSource<int>>() {
                        MaybeSource.Just(1),
                        null,
                        MaybeSource.Empty<int>(),
                        MaybeSource.Just(2)
                    }, maxConcurrency: i
                )
                .Test()
                .AssertNotCompleted()
                .AssertError(typeof(NullReferenceException));
            }
        }

        [Test]
        public void Enumerable_Limited_Dispose()
        {
            var ms1 = new MaybeSubject<int>();
            var ms2 = new MaybeSubject<int>();

            var to = new List<IMaybeSource<int>>()
            {
                ms1,
                ms2
            }
            .Merge(maxConcurrency: 2)
            .Test();

            Assert.True(ms1.HasObserver());
            Assert.True(ms2.HasObserver());

            to.Dispose();

            Assert.False(ms1.HasObserver());
            Assert.False(ms2.HasObserver());
        }

        [Test]
        public void Enumerable_Limited_Dispose_On_Error()
        {
            var ms1 = new MaybeSubject<int>();
            var ms2 = new MaybeSubject<int>();

            var to = new List<IMaybeSource<int>>()
            {
                ms1,
                ms2
            }
            .Merge(maxConcurrency: 2)
            .Test();

            Assert.True(ms1.HasObserver());
            Assert.True(ms2.HasObserver());

            ms2.OnError(new InvalidOperationException());

            Assert.False(ms1.HasObserver());
            Assert.False(ms2.HasObserver());

            to.AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Enumerable_Limited_Subscription()
        {
            var ms1 = new MaybeSubject<int>();
            var ms2 = new MaybeSubject<int>();

            var to = new List<IMaybeSource<int>>()
            {
                ms1,
                ms2
            }
            .Merge(maxConcurrency: 1)
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
        public void Enumerable_Limited_Subscription_Empty()
        {
            var ms1 = new MaybeSubject<int>();
            var ms2 = new MaybeSubject<int>();

            var to = new List<IMaybeSource<int>>()
            {
                ms1,
                ms2
            }
            .Merge(maxConcurrency: 1)
            .Test();

            Assert.True(ms1.HasObserver());
            Assert.False(ms2.HasObserver());

            ms1.OnCompleted();

            Assert.False(ms1.HasObserver());
            Assert.True(ms2.HasObserver());

            ms2.OnCompleted();

            to.AssertResult();
        }

        [Test]
        public void Enumerable_Limited_Race_Complete()
        {
            for (int k = 1; k < 4; k++)
            {
                for (int i = 0; i < TestHelper.RACE_LOOPS; i++)
                {
                    var ms1 = new MaybeSubject<int>();
                    var ms2 = new MaybeSubject<int>();

                    var to = new List<IMaybeSource<int>>()
                    {
                        ms1,
                        ms2
                    }
                    .Merge(maxConcurrency: k)
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
        public void Enumerable_Limited_Race_Success()
        {
            for (int k = 1; k < 4; k++)
            {
                for (int i = 0; i < TestHelper.RACE_LOOPS; i++)
                {
                    var ms1 = new MaybeSubject<int>();
                    var ms2 = new MaybeSubject<int>();

                    var to = new List<IMaybeSource<int>>()
                    {
                        ms1,
                        ms2
                    }
                    .Merge(maxConcurrency: k)
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
        public void Enumerable_Limited_Race_Empty_Success()
        {
            for (int k = 1; k < 4; k++)
            {
                for (int i = 0; i < TestHelper.RACE_LOOPS; i++)
                {
                    var ms1 = new MaybeSubject<int>();
                    var ms2 = new MaybeSubject<int>();

                    var to = new List<IMaybeSource<int>>()
                    {
                        ms1,
                        ms2
                    }
                    .Merge(maxConcurrency: k)
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

        public void Enumerable_Limited_GetEnumerator_Crash()
        {
            MaybeSource.Merge(new FailingEnumerable<IMaybeSource<int>>(true, false, false), maxConcurrency: 1)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        public void Enumerable_Limited_MoveNext_Crash()
        {
            MaybeSource.Merge(new FailingEnumerable<IMaybeSource<int>>(false, true, false), maxConcurrency: 1)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        public void Enumerable_Limited_GetEnumerator_Crash_DelayErrors()
        {
            MaybeSource.Merge(new FailingEnumerable<IMaybeSource<int>>(true, false, false), true, 1)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        public void Enumerable_Limited_MoveNext_Crash_DelayErrors()
        {
            MaybeSource.Merge(new FailingEnumerable<IMaybeSource<int>>(false, true, false), true, 1)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }


        #endregion + Enumerable +

    }
}
