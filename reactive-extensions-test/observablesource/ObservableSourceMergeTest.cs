using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Collections.Generic;
using System.Reactive.Concurrency;

namespace akarnokd.reactive_extensions_test.observablesource
{
    [TestFixture]
    public class ObservableSourceMergeTest
    {
        [Test]
        public void Max_Array_Basic()
        {
            ObservableSource.Merge(
                ObservableSource.Range(1, 5),
                ObservableSource.Range(6, 5),
                ObservableSource.Range(11, 5),
                ObservableSource.Range(16, 5)
            )
            .Test()
            .AssertResult(
                1, 2, 3, 4, 5,
                6, 7, 8, 9, 10,
                11, 12, 13, 14, 15,
                16, 17, 18, 19, 20
            );
        }

        [Test]
        public void Max_Array_Error()
        {
            ObservableSource.Merge(
                ObservableSource.Range(1, 5),
                ObservableSource.Error<int>(new InvalidOperationException()),
                ObservableSource.Range(11, 5),
                ObservableSource.Range(16, 5)
            )
            .Test()
            .AssertFailure(typeof(InvalidOperationException),
                1, 2, 3, 4, 5
            );
        }

        [Test]
        public void Max_Array_Null()
        {
            ObservableSource.Merge(
                ObservableSource.Range(1, 5),
                null,
                ObservableSource.Range(11, 5),
                ObservableSource.Range(16, 5)
            )
            .Test()
            .AssertFailure(typeof(NullReferenceException),
                1, 2, 3, 4, 5
            );
        }

        [Test]
        public void Max_Array_Error_Delayed()
        {
            ObservableSource.Merge(true,
                ObservableSource.Range(1, 5),
                ObservableSource.Error<int>(new InvalidOperationException()),
                ObservableSource.Range(11, 5),
                ObservableSource.Range(16, 5)
            )
            .Test()
            .AssertFailure(typeof(InvalidOperationException),
                1, 2, 3, 4, 5, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20
            );
        }

        [Test]
        public void Max_Array_Dispose()
        {
            TestHelper.VerifyDisposeObservableSource<int, int>(o => ObservableSource.Merge(o, o, o));
        }

        [Test]
        public void Max_Array_Long_Async()
        {
            ObservableSource.Merge(
                ObservableSource.Range(1, 1000).SubscribeOn(ThreadPoolScheduler.Instance),
                ObservableSource.Range(1001, 1000).SubscribeOn(ThreadPoolScheduler.Instance)
            )
            .Test()
            .AwaitDone(TimeSpan.FromSeconds(5))
            .AssertValueCount(2000)
            .AssertCompleted()
            .AssertNoError();
        }

        [Test]
        public void Max_Enumerable_Basic()
        {
            ObservableSource.Merge(
                new List<IObservableSource<int>>()
                {
                    ObservableSource.Range(1, 5),
                    ObservableSource.Range(6, 5),
                    ObservableSource.Range(11, 5),
                    ObservableSource.Range(16, 5)
                }
            )
            .Test()
            .AssertResult(
                1, 2, 3, 4, 5,
                6, 7, 8, 9, 10,
                11, 12, 13, 14, 15,
                16, 17, 18, 19, 20
            );
        }

        [Test]
        public void Max_Enumerable_Error()
        {
            ObservableSource.Merge(
                new List<IObservableSource<int>>()
                {
                    ObservableSource.Range(1, 5),
                    ObservableSource.Error<int>(new InvalidOperationException()),
                    ObservableSource.Range(11, 5),
                    ObservableSource.Range(16, 5)
                }
            )
            .Test()
            .AssertFailure(typeof(InvalidOperationException),
                1, 2, 3, 4, 5
            );
        }

        [Test]
        public void Max_Enumerable_Null()
        {
            ObservableSource.Merge(
                new List<IObservableSource<int>>()
                {
                    ObservableSource.Range(1, 5),
                    null,
                    ObservableSource.Range(11, 5),
                    ObservableSource.Range(16, 5)
                }
            )
            .Test()
            .AssertFailure(typeof(NullReferenceException),
                1, 2, 3, 4, 5
            );
        }

        [Test]
        public void Max_Enumerable_GetEnumerator_Crash()
        {
            ObservableSource.Merge(
                new FailingEnumerable<IObservableSource<int>>(true, false, false)
            )
            .Test()
            .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Max_Enumerable_MoveNext_Crash()
        {
            ObservableSource.Merge(
                new FailingEnumerable<IObservableSource<int>>(false, true, false)
            )
            .Test()
            .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Max_Enumerable_Error_Delayed()
        {
            ObservableSource.Merge(
                new List<IObservableSource<int>>()
                {
                    ObservableSource.Range(1, 5),
                    ObservableSource.Error<int>(new InvalidOperationException()),
                    ObservableSource.Range(11, 5),
                    ObservableSource.Range(16, 5)
                }
            , true)
            .Test()
            .AssertFailure(typeof(InvalidOperationException),
                1, 2, 3, 4, 5, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20
            );
        }

        [Test]
        public void Max_Enumerable_Dispose()
        {
            TestHelper.VerifyDisposeObservableSource<int, int>(o => ObservableSource.Merge(
                new List<IObservableSource<int>>()
                {
                    o, o, o
                }
            ));
        }

        [Test]
        public void Max_Enumerable_Long_Async()
        {
            ObservableSource.Merge(
                new List<IObservableSource<int>>()
                {
                    ObservableSource.Range(1, 1000).SubscribeOn(ThreadPoolScheduler.Instance),
                    ObservableSource.Range(1001, 1000).SubscribeOn(ThreadPoolScheduler.Instance)
                }
            )
            .Test()
            .AwaitDone(TimeSpan.FromSeconds(5))
            .AssertValueCount(2000)
            .AssertCompleted()
            .AssertNoError();
        }

        [Test]
        public void Limited_Array_Basic()
        {
            for (int i = 1; i < 7; i++)
            {
                ObservableSource.Merge(i,
                    ObservableSource.Range(1, 5),
                    ObservableSource.Range(6, 5),
                    ObservableSource.Range(11, 5),
                    ObservableSource.Range(16, 5)
                )
                .Test()
                .AssertResult(
                    1, 2, 3, 4, 5,
                    6, 7, 8, 9, 10,
                    11, 12, 13, 14, 15,
                    16, 17, 18, 19, 20
                );
            }
        }

        [Test]
        public void Limited_Array_Error()
        {
            for (int i = 1; i < 7; i++)
            {
                ObservableSource.Merge(i,
                    ObservableSource.Range(1, 5),
                    ObservableSource.Error<int>(new InvalidOperationException()),
                    ObservableSource.Range(11, 5),
                    ObservableSource.Range(16, 5)
                )
                .Test()
                .WithTag($"maxConcurrency: {i}")
                .AssertSubscribed()
                .AssertError(typeof(InvalidOperationException))
                ;
            }
        }

        [Test]
        public void Limited_Array_Null()
        {
            for (int i = 1; i < 7; i++)
            {
                ObservableSource.Merge(i,
                    ObservableSource.Range(1, 5),
                    null,
                    ObservableSource.Range(11, 5),
                    ObservableSource.Range(16, 5)
                )
                .Test()
                .WithTag($"maxConcurrency: {i}")
                .AssertSubscribed()
                .AssertError(typeof(NullReferenceException))
                ;
            }
        }

        [Test]
        public void Limited_Array_Error_Delayed()
        {
            for (int i = 1; i < 7; i++)
            {
                ObservableSource.Merge(true, i,
                    ObservableSource.Range(1, 5),
                    ObservableSource.Error<int>(new InvalidOperationException()),
                    ObservableSource.Range(11, 5),
                    ObservableSource.Range(16, 5)
                )
                .Test()
                .AssertFailure(typeof(InvalidOperationException),
                    1, 2, 3, 4, 5, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20
                );
            }
        }

        [Test]
        public void Limited_Array_Dispose()
        {
            for (int i = 1; i < 7; i++)
            {
                TestHelper.VerifyDisposeObservableSource<int, int>(o => ObservableSource.Merge(i, o, o, o));
            }
        }

        [Test]
        public void Limited_Array_Long_Async()
        {
            for (int i = 1; i < 7; i++)
            {
                ObservableSource.Merge(i,
                    ObservableSource.Range(1, 1000).SubscribeOn(ThreadPoolScheduler.Instance),
                    ObservableSource.Range(1001, 1000).SubscribeOn(ThreadPoolScheduler.Instance)
                )
                .Test()
                .AwaitDone(TimeSpan.FromSeconds(5))
                .AssertValueCount(2000)
                .AssertCompleted()
                .AssertNoError();
            }
        }

        [Test]
        public void Limited_Array_Step()
        {
            var subj1 = new PublishSubject<int>();
            var subj2 = new PublishSubject<int>();

            var to = ObservableSource.Merge(1, subj1, subj2)
                .Test();

            to.AssertEmpty();

            Assert.True(subj1.HasObservers);
            Assert.False(subj2.HasObservers);

            subj1.OnCompleted();

            to.AssertEmpty();

            Assert.False(subj1.HasObservers);
            Assert.True(subj2.HasObservers);

            subj2.OnCompleted();

            Assert.False(subj1.HasObservers);
            Assert.False(subj2.HasObservers);

            to.AssertResult();
        }

        [Test]
        public void Limited_Enumerable_Basic()
        {
            for (int i = 1; i < 7; i++)
            {
                ObservableSource.Merge(
                    new List<IObservableSource<int>>()
                    {
                        ObservableSource.Range(1, 5),
                        ObservableSource.Range(6, 5),
                        ObservableSource.Range(11, 5),
                        ObservableSource.Range(16, 5)
                    }
                , maxConcurrency: i)
                .Test()
                .AssertResult(
                    1, 2, 3, 4, 5,
                    6, 7, 8, 9, 10,
                    11, 12, 13, 14, 15,
                    16, 17, 18, 19, 20
                );
            }
        }

        [Test]
        public void Limited_Enumerable_Error()
        {
            for (int i = 1; i < 7; i++)
            {
                ObservableSource.Merge(
                    new List<IObservableSource<int>>()
                    {
                        ObservableSource.Range(1, 5),
                        ObservableSource.Error<int>(new InvalidOperationException()),
                        ObservableSource.Range(11, 5),
                        ObservableSource.Range(16, 5)
                    }
                , maxConcurrency: i)
                .Test()
                .WithTag($"maxConcurrency: {i}")
                .AssertSubscribed()
                .AssertError(typeof(InvalidOperationException))
                ;
            }
        }

        [Test]
        public void Limited_Enumerable_Null()
        {
            for (int i = 1; i < 7; i++)
            {
                ObservableSource.Merge(
                    new List<IObservableSource<int>>()
                    {
                        ObservableSource.Range(1, 5),
                        null,
                        ObservableSource.Range(11, 5),
                        ObservableSource.Range(16, 5)
                    }
                , maxConcurrency: i)
                .Test()
                .WithTag($"maxConcurrency: {i}")
                .AssertSubscribed()
                .AssertError(typeof(NullReferenceException))
                ;
            }
        }

        [Test]
        public void Limited_Enumerable_GetEnumerator_Crash()
        {
            for (int i = 1; i < 7; i++)
            {
                ObservableSource.Merge(
                    new FailingEnumerable<IObservableSource<int>>(true, false, false)
                , maxConcurrency: i)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
            }
        }

        [Test]
        public void Limited_Enumerable_MoveNext_Crash()
        {
            for (int i = 1; i < 7; i++)
            {
                ObservableSource.Merge(
                    new FailingEnumerable<IObservableSource<int>>(false, true, false)
                , maxConcurrency: i)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
            }
        }

        [Test]
        public void Limited_Enumerable_Error_Delayed()
        {
            for (int i = 1; i < 7; i++)
            {
                ObservableSource.Merge(
                    new List<IObservableSource<int>>()
                    {
                        ObservableSource.Range(1, 5),
                        ObservableSource.Error<int>(new InvalidOperationException()),
                        ObservableSource.Range(11, 5),
                        ObservableSource.Range(16, 5)
                    }
                , true, i)
                .Test()
                .AssertFailure(typeof(InvalidOperationException),
                    1, 2, 3, 4, 5, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20
                );
            }
        }

        [Test]
        public void Limited_Enumerable_Dispose()
        {
            for (int i = 1; i < 7; i++)
            {
                TestHelper.VerifyDisposeObservableSource<int, int>(o => ObservableSource.Merge(
                    new List<IObservableSource<int>>()
                    {
                        o, o, o
                    }, maxConcurrency: i
                ));
            }
        }

        [Test]
        public void Limited_Enumerable_Long_Async()
        {
            for (int i = 1; i < 7; i++)
            {
                ObservableSource.Merge(
                    new List<IObservableSource<int>>()
                    {
                        ObservableSource.Range(1, 1000).SubscribeOn(ThreadPoolScheduler.Instance),
                        ObservableSource.Range(1001, 1000).SubscribeOn(ThreadPoolScheduler.Instance)
                    }, maxConcurrency: i
                )
                .Test()
                .AwaitDone(TimeSpan.FromSeconds(5))
                .AssertValueCount(2000)
                .AssertCompleted()
                .AssertNoError();
            }
        }
    }
}
