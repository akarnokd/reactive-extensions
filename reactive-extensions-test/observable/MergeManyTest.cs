using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Reactive.Linq;

namespace akarnokd.reactive_extensions_test.observable
{
    [TestFixture]
    public class MergeManyTest
    {
        [Test]
        public void Basic()
        {
            Observable.Range(1, 5)
                .Select(v => Observable.Range(v * 100, 5))
                .MergeMany()
                .Test()
                .AssertValueCount(25)
                .AssertNoError()
                .AssertCompleted();
        }

        [Test]
        public void Basic_DelayErrors()
        {
            Observable.Range(1, 5)
                .Select(v => Observable.Range(v * 100, 5))
                .MergeMany(delayErrors: true)
                .Test()
                .AssertValueCount(25)
                .AssertNoError()
                .AssertCompleted();
        }

        [Test]
        public void Basic_MaxConcurrency()
        {
            Observable.Range(1, 5)
                .Select(v => Observable.Range(v * 100, 5))
                .MergeMany(maxConcurrency: 1)
                .Test()
                .AssertValueCount(25)
                .AssertNoError()
                .AssertCompleted();
        }

        [Test]
        public void Basic_DelayErrors_MaxConcurrency()
        {
            Observable.Range(1, 5)
                .Select(v => Observable.Range(v * 100, 5))
                .MergeMany(delayErrors: true, maxConcurrency: 1)
                .Test()
                .AssertValueCount(25)
                .AssertNoError()
                .AssertCompleted();
        }

        [Test]
        public void DelayError_Main()
        {
            Observable.Range(1, 5).Concat(Observable.Throw<int>(new InvalidOperationException()))
                .Select(v => {
                    return Observable.Range(v * 100, 5);
                })
                .MergeMany(delayErrors: true)
                .Test()
                .AssertValueCount(25)
                .AssertError(typeof(InvalidOperationException))
                .AssertNotCompleted();
        }

        [Test]
        public void DelayError_Inner()
        {
            Observable.Range(1, 5)
                .Select(v => {
                    if (v == 3)
                    {
                        return Observable.Throw<int>(new InvalidOperationException());
                    }
                    return Observable.Range(v * 100, 5);
                })
                .MergeMany(delayErrors: true)
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
                var ups = new UnicastSubject<int>[]
                {
                    new UnicastSubject<int>(),
                    new UnicastSubject<int>()
                };

                var to = Observable.Range(0, 2)
                    .Select(v => ups[v])
                    .MergeMany()
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
                var ups = new UnicastSubject<int>[]
                {
                    new UnicastSubject<int>(),
                    new UnicastSubject<int>()
                };

                var to = Observable.Range(0, 2)
                    .Select(v => ups[v])
                    .MergeMany(delayErrors: true)
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
                var ups = new UnicastSubject<int>[]
                {
                    new UnicastSubject<int>(),
                    new UnicastSubject<int>()
                };

                var to = Observable.Range(0, 2)
                    .Select(v => ups[v])
                    .MergeMany(maxConcurrency: 1)
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
                var ups = new UnicastSubject<int>[]
                {
                    new UnicastSubject<int>(),
                    new UnicastSubject<int>()
                };

                var to = Observable.Range(0, 2)
                    .Select(v => ups[v])
                    .MergeMany(maxConcurrency: 2)
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
                var ups = new UnicastSubject<int>[]
                {
                    new UnicastSubject<int>(),
                    new UnicastSubject<int>()
                };

                var to = Observable.Range(0, 2)
                    .Select(v => ups[v])
                    .MergeMany(delayErrors: true, maxConcurrency: 1)
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
                var ups = new UnicastSubject<int>[]
                {
                    new UnicastSubject<int>(),
                    new UnicastSubject<int>()
                };

                var to = Observable.Range(0, 2)
                    .Select(v => ups[v])
                    .MergeMany(delayErrors: true, maxConcurrency: 2)
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
            var us = new UnicastSubject<int>[]
            {
                new UnicastSubject<int>(),
                new UnicastSubject<int>(),
                new UnicastSubject<int>(),
            };

            var to = Observable.Range(0, 3)
                .Select(v => us[v])
                .MergeMany(maxConcurrency: 1)
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
            var us = new UnicastSubject<int>[]
            {
                new UnicastSubject<int>(),
                new UnicastSubject<int>(),
                new UnicastSubject<int>(),
            };

            var to = Observable.Range(0, 3)
                .Select(v => us[v])
                .MergeMany(maxConcurrency: 2)
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
            var us = new UnicastSubject<int>[]
            {
                new UnicastSubject<int>(),
                new UnicastSubject<int>(),
                new UnicastSubject<int>(),
            };

            var to = Observable.Range(0, 3)
                .Select(v => us[v])
                .MergeMany(delayErrors: true, maxConcurrency: 1)
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
            var us = new UnicastSubject<int>[]
            {
                new UnicastSubject<int>(),
                new UnicastSubject<int>(),
                new UnicastSubject<int>(),
            };

            var to = Observable.Range(0, 3)
                .Select(v => us[v])
                .MergeMany(delayErrors: true, maxConcurrency: 2)
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
    }
}

