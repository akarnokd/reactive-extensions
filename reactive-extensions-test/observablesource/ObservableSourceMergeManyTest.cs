using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.observablesource
{
    [TestFixture]
    public class ObservableSourceMergeManyTest
    {
        [Test]
        public void Basic()
        {
            ObservableSource.Range(1, 5)
                .Map(v => ObservableSource.Range(v * 100, 5))
                .Merge()
                .Test()
                .AssertValueCount(25)
                .AssertNoError()
                .AssertCompleted();
        }

        [Test]
        public void Basic_DelayErrors()
        {
            ObservableSource.Range(1, 5)
                .Map(v => ObservableSource.Range(v * 100, 5))
                .Merge(delayErrors: true)
                .Test()
                .AssertValueCount(25)
                .AssertNoError()
                .AssertCompleted();
        }

        [Test]
        public void Basic_MaxConcurrency()
        {
            ObservableSource.Range(1, 5)
                .Map(v => ObservableSource.Range(v * 100, 5))
                .Merge(maxConcurrency: 1)
                .Test()
                .AssertValueCount(25)
                .AssertNoError()
                .AssertCompleted();
        }

        [Test]
        public void Basic_DelayErrors_MaxConcurrency()
        {
            ObservableSource.Range(1, 5)
                .Map(v => ObservableSource.Range(v * 100, 5))
                .Merge(delayErrors: true, maxConcurrency: 1)
                .Test()
                .AssertValueCount(25)
                .AssertNoError()
                .AssertCompleted();
        }

        [Test]
        public void DelayError_Main()
        {
            ObservableSource.Range(1, 5).Concat(ObservableSource.Error<int>(new InvalidOperationException()))
                .Map(v => {
                    return ObservableSource.Range(v * 100, 5);
                })
                .Merge(delayErrors: true)
                .Test()
                .AssertValueCount(25)
                .AssertError(typeof(InvalidOperationException))
                .AssertNotCompleted();
        }

        [Test]
        public void DelayError_Inner()
        {
            ObservableSource.Range(1, 5)
                .Map(v => {
                    if (v == 3)
                    {
                        return ObservableSource.Error<int>(new InvalidOperationException());
                    }
                    return ObservableSource.Range(v * 100, 5);
                })
                .Merge(delayErrors: true)
                .Test()
                .AssertValueCount(20)
                .AssertError(typeof(InvalidOperationException))
                .AssertNotCompleted();
        }

        [Test]
        public void Race()
        {
            for (int i = 0; i < TestHelper.RACE_LOOPS; i++)
            {
                var ups = new MonocastSubject<int>[]
                {
                    new MonocastSubject<int>(),
                    new MonocastSubject<int>()
                };

                var to = ObservableSource.Range(0, 2)
                    .Map(v => ups[v])
                    .Merge()
                    .Test();

                TestHelper.Race(() => {
                    for (int j = 0; j < 1000; j++)
                    {
                        ups[0].OnNext(j);
                    }
                    ups[0].OnCompleted();
                }, () => {
                    for (int j = 1000; j < 2000; j++)
                    {
                        ups[1].OnNext(j);
                    }
                    ups[1].OnCompleted();
                });

                to.AssertValueCount(2000)
                    .AssertNoError()
                    .AssertCompleted();
            }
        }

        [Test]
        public void Race_DelayErrors()
        {
            for (int i = 0; i < TestHelper.RACE_LOOPS; i++)
            {
                var ups = new MonocastSubject<int>[]
                {
                    new MonocastSubject<int>(),
                    new MonocastSubject<int>()
                };

                var to = ObservableSource.Range(0, 2)
                    .Map(v => ups[v])
                    .Merge(delayErrors: true)
                    .Test();

                TestHelper.Race(() => {
                    for (int j = 0; j < 1000; j++)
                    {
                        ups[0].OnNext(j);
                    }
                    ups[0].OnCompleted();
                }, () => {
                    for (int j = 1000; j < 2000; j++)
                    {
                        ups[1].OnNext(j);
                    }
                    ups[1].OnCompleted();
                });

                to.AssertValueCount(2000)
                    .AssertNoError()
                    .AssertCompleted();
            }
        }

        [Test]
        public void Race_MaxConcurrency_1()
        {
            for (int i = 0; i < TestHelper.RACE_LOOPS; i++)
            {
                var ups = new MonocastSubject<int>[]
                {
                    new MonocastSubject<int>(),
                    new MonocastSubject<int>()
                };

                var to = ObservableSource.Range(0, 2)
                    .Map(v => ups[v])
                    .Merge(maxConcurrency: 1)
                    .Test();

                TestHelper.Race(() => {
                    for (int j = 0; j < 1000; j++)
                    {
                        ups[0].OnNext(j);
                    }
                    ups[0].OnCompleted();
                }, () => {
                    for (int j = 1000; j < 2000; j++)
                    {
                        ups[1].OnNext(j);
                    }
                    ups[1].OnCompleted();
                });

                to.AssertValueCount(2000)
                    .AssertNoError()
                    .AssertCompleted();
            }
        }

        [Test]
        public void Race_MaxConcurrency_2()
        {
            for (int i = 0; i < TestHelper.RACE_LOOPS; i++)
            {
                var ups = new MonocastSubject<int>[]
                {
                    new MonocastSubject<int>(),
                    new MonocastSubject<int>()
                };

                var to = ObservableSource.Range(0, 2)
                    .Map(v => ups[v])
                    .Merge(maxConcurrency: 2)
                    .Test();

                TestHelper.Race(() =>
                {
                    for (int j = 0; j < 1000; j++)
                    {
                        ups[0].OnNext(j);
                    }
                    ups[0].OnCompleted();
                }, () =>
                {
                    for (int j = 1000; j < 2000; j++)
                    {
                        ups[1].OnNext(j);
                    }
                    ups[1].OnCompleted();
                });

                to.AssertValueCount(2000)
                    .AssertNoError()
                    .AssertCompleted();
            }
        }


        [Test]
        public void Race_MaxConcurrency_1_DelayErrors()
        {
            for (int i = 0; i < TestHelper.RACE_LOOPS; i++)
            {
                var ups = new MonocastSubject<int>[]
                {
                    new MonocastSubject<int>(),
                    new MonocastSubject<int>()
                };

                var to = ObservableSource.Range(0, 2)
                    .Map(v => ups[v])
                    .Merge(delayErrors: true, maxConcurrency: 1)
                    .Test();

                TestHelper.Race(() => {
                    for (int j = 0; j < 1000; j++)
                    {
                        ups[0].OnNext(j);
                    }
                    ups[0].OnCompleted();
                }, () => {
                    for (int j = 1000; j < 2000; j++)
                    {
                        ups[1].OnNext(j);
                    }
                    ups[1].OnCompleted();
                });

                to.AssertValueCount(2000)
                    .AssertNoError()
                    .AssertCompleted();
            }
        }

        [Test]
        public void Race_MaxConcurrency_2_DelayErrors()
        {
            for (int i = 0; i < TestHelper.RACE_LOOPS; i++)
            {
                var ups = new MonocastSubject<int>[]
                {
                    new MonocastSubject<int>(),
                    new MonocastSubject<int>()
                };

                var to = ObservableSource.Range(0, 2)
                    .Map(v => ups[v])
                    .Merge(delayErrors: true, maxConcurrency: 2)
                    .Test();

                TestHelper.Race(() =>
                {
                    for (int j = 0; j < 1000; j++)
                    {
                        ups[0].OnNext(j);
                    }
                    ups[0].OnCompleted();
                }, () =>
                {
                    for (int j = 1000; j < 2000; j++)
                    {
                        ups[1].OnNext(j);
                    }
                    ups[1].OnCompleted();
                });

                to.AssertValueCount(2000)
                    .AssertNoError()
                    .AssertCompleted();
            }
        }

        [Test]
        public void Max_Concurrency_Honored()
        {
            var us = new MonocastSubject<int>[]
            {
                new MonocastSubject<int>(),
                new MonocastSubject<int>(),
                new MonocastSubject<int>(),
            };

            var to = ObservableSource.Range(0, 3)
                .Map(v => us[v])
                .Merge(maxConcurrency: 1)
                .Test();

            to.AssertEmpty();

            Assert.True(us[0].HasObserver());
            Assert.False(us[1].HasObserver());
            Assert.False(us[2].HasObserver());

            us[0].OnNext(1);

            Assert.True(us[0].HasObserver());
            Assert.False(us[1].HasObserver());
            Assert.False(us[2].HasObserver());

            to.AssertValuesOnly(1);

            us[0].OnCompleted();

            Assert.False(us[0].HasObserver());
            Assert.True(us[1].HasObserver());
            Assert.False(us[2].HasObserver());

            us[1].OnNext(2);

            to.AssertValuesOnly(1, 2);

            us[1].OnCompleted();

            Assert.False(us[0].HasObserver());
            Assert.False(us[1].HasObserver());
            Assert.True(us[2].HasObserver());

            us[2].OnNext(3);

            to.AssertValuesOnly(1, 2, 3);

            us[2].OnCompleted();

            Assert.False(us[0].HasObserver());
            Assert.False(us[1].HasObserver());
            Assert.False(us[2].HasObserver());

            to.AssertResult(1, 2, 3);
        }

        [Test]
        public void Max_Concurrency_Honored_2()
        {
            var us = new MonocastSubject<int>[]
            {
                new MonocastSubject<int>(),
                new MonocastSubject<int>(),
                new MonocastSubject<int>(),
            };

            var to = ObservableSource.Range(0, 3)
                .Map(v => us[v])
                .Merge(maxConcurrency: 2)
                .Test();

            to.AssertEmpty();

            Assert.True(us[0].HasObserver());
            Assert.True(us[1].HasObserver());
            Assert.False(us[2].HasObserver());

            us[0].OnNext(1);

            Assert.True(us[0].HasObserver());
            Assert.True(us[1].HasObserver());
            Assert.False(us[2].HasObserver());

            to.AssertValuesOnly(1);

            us[0].OnCompleted();

            Assert.False(us[0].HasObserver());
            Assert.True(us[1].HasObserver());
            Assert.True(us[2].HasObserver());

            us[1].OnNext(2);

            to.AssertValuesOnly(1, 2);

            us[1].OnCompleted();

            Assert.False(us[0].HasObserver());
            Assert.False(us[1].HasObserver());
            Assert.True(us[2].HasObserver());

            us[2].OnNext(3);

            to.AssertValuesOnly(1, 2, 3);

            us[2].OnCompleted();

            Assert.False(us[0].HasObserver());
            Assert.False(us[1].HasObserver());
            Assert.False(us[2].HasObserver());

            to.AssertResult(1, 2, 3);
        }

        [Test]
        public void Max_Concurrency_Honored_DelayErrors()
        {
            var us = new MonocastSubject<int>[]
            {
                new MonocastSubject<int>(),
                new MonocastSubject<int>(),
                new MonocastSubject<int>(),
            };

            var to = ObservableSource.Range(0, 3)
                .Map(v => us[v])
                .Merge(delayErrors: true, maxConcurrency: 1)
                .Test();

            to.AssertEmpty();

            Assert.True(us[0].HasObserver());
            Assert.False(us[1].HasObserver());
            Assert.False(us[2].HasObserver());

            us[0].OnNext(1);

            Assert.True(us[0].HasObserver());
            Assert.False(us[1].HasObserver());
            Assert.False(us[2].HasObserver());

            to.AssertValuesOnly(1);

            us[0].OnCompleted();

            Assert.False(us[0].HasObserver());
            Assert.True(us[1].HasObserver());
            Assert.False(us[2].HasObserver());

            us[1].OnNext(2);

            to.AssertValuesOnly(1, 2);

            us[1].OnCompleted();

            Assert.False(us[0].HasObserver());
            Assert.False(us[1].HasObserver());
            Assert.True(us[2].HasObserver());

            us[2].OnNext(3);

            to.AssertValuesOnly(1, 2, 3);

            us[2].OnCompleted();

            Assert.False(us[0].HasObserver());
            Assert.False(us[1].HasObserver());
            Assert.False(us[2].HasObserver());

            to.AssertResult(1, 2, 3);
        }

        [Test]
        public void Max_Concurrency_Honored_2_DelayErrors()
        {
            var us = new MonocastSubject<int>[]
            {
                new MonocastSubject<int>(),
                new MonocastSubject<int>(),
                new MonocastSubject<int>(),
            };

            var to = ObservableSource.Range(0, 3)
                .Map(v => us[v])
                .Merge(delayErrors: true, maxConcurrency: 2)
                .Test();

            to.AssertEmpty();

            Assert.True(us[0].HasObserver());
            Assert.True(us[1].HasObserver());
            Assert.False(us[2].HasObserver());

            us[0].OnNext(1);

            Assert.True(us[0].HasObserver());
            Assert.True(us[1].HasObserver());
            Assert.False(us[2].HasObserver());

            to.AssertValuesOnly(1);

            us[0].OnCompleted();

            Assert.False(us[0].HasObserver());
            Assert.True(us[1].HasObserver());
            Assert.True(us[2].HasObserver());

            us[1].OnNext(2);

            to.AssertValuesOnly(1, 2);

            us[1].OnCompleted();

            Assert.False(us[0].HasObserver());
            Assert.False(us[1].HasObserver());
            Assert.True(us[2].HasObserver());

            us[2].OnNext(3);

            to.AssertValuesOnly(1, 2, 3);

            us[2].OnCompleted();

            Assert.False(us[0].HasObserver());
            Assert.False(us[1].HasObserver());
            Assert.False(us[2].HasObserver());

            to.AssertResult(1, 2, 3);
        }

        [Test]
        public void Max_Dispose()
        {
            var subj1 = new PublishSubject<int>();
            var subj2 = new PublishSubject<int>();

            var to = ObservableSource.FromArray(subj1, subj2).Merge<int>().Test();

            to.AssertEmpty();

            Assert.True(subj1.HasObservers);
            Assert.True(subj2.HasObservers);

            to.Dispose();

            Assert.False(subj1.HasObservers);
            Assert.False(subj2.HasObservers);
        }

        [Test]
        public void Limited_Dispose()
        {
            var subj1 = new PublishSubject<int>();
            var subj2 = new PublishSubject<int>();

            var to = ObservableSource.FromArray(subj1, subj2).Merge<int>(maxConcurrency: 2).Test();

            to.AssertEmpty();

            Assert.True(subj1.HasObservers);
            Assert.True(subj2.HasObservers);

            to.Dispose();

            Assert.False(subj1.HasObservers);
            Assert.False(subj2.HasObservers);
        }

        [Test]
        public void Limited_Error_Main()
        {
            var to = ObservableSource.Error<IObservableSource<int>>(new InvalidOperationException())
                .Merge()
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Limited_Error_Main_Delayed()
        {
            var to = ObservableSource.Error<IObservableSource<int>>(new InvalidOperationException())
                .Merge(true)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Limited_Error_Inner()
        {
            var to = ObservableSource.Just<IObservableSource<int>>(
                    ObservableSource.Error<int>(new InvalidOperationException())).Hide()
                .Merge(maxConcurrency: 1)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Limited_Error_Inner_Delayed()
        {
            var to = ObservableSource.Just<IObservableSource<int>>(
                    ObservableSource.Error<int>(new InvalidOperationException())).Hide()
                .Merge(true, 1)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }
    }
}

