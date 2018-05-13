using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Reactive.Linq;

namespace akarnokd.reactive_extensions_test
{
    [TestFixture]
    public class ZipTest
    {
        [Test]
        public void Basic()
        {
            ReactiveExtensions.Zip(a =>
            {
                int s = 0;
                foreach (var v in a)
                {
                    s += v;
                }
                return s;
            },
                Observable.Range(1, 5),
                Observable.Range(10, 5)
            ).Test()
            .AssertResult(10 + 1, 11 + 2, 12 + 3, 13 + 4, 14 + 5);
        }

        [Test]
        public void Basic_DelayErrors()
        {
            ReactiveExtensions.Zip(a =>
            {
                int s = 0;
                foreach (var v in a)
                {
                    s += v;
                }
                return s;
            }, true,
                Observable.Range(1, 5),
                Observable.Range(10, 5)
            ).Test()
            .AssertResult(10 + 1, 11 + 2, 12 + 3, 13 + 4, 14 + 5);
        }

        [Test]
        public void Basic_Array()
        {
            new []
            {
                Observable.Range(1, 5),
                Observable.Range(10, 5)
            }
            .Zip(a =>
            {
                int s = 0;
                foreach (var v in a)
                {
                    s += v;
                }
                return s;
            }
            ).Test()
            .AssertResult(10 + 1, 11 + 2, 12 + 3, 13 + 4, 14 + 5);
        }

        [Test]
        public void First_Shorter()
        {
            ReactiveExtensions.Zip(a =>
            {
                int s = 0;
                foreach (var v in a)
                {
                    s += v;
                }
                return s;
            },
                Observable.Range(1, 4),
                Observable.Range(10, 5)
            ).Test()
            .AssertResult(10 + 1, 11 + 2, 12 + 3, 13 + 4);
        }

        [Test]
        public void Second_Shorter()
        {
            ReactiveExtensions.Zip(a =>
            {
                int s = 0;
                foreach (var v in a)
                {
                    s += v;
                }
                return s;
            },
                Observable.Range(1, 5),
                Observable.Range(10, 4)
            ).Test()
            .AssertResult(10 + 1, 11 + 2, 12 + 3, 13 + 4);
        }

        [Test]
        public void Error_First_Source()
        {
            var us1 = new UnicastSubject<int>();
            var us2 = new UnicastSubject<int>();

            var to = ReactiveExtensions.Zip(a =>
            {
                int s = 0;
                foreach (var v in a)
                {
                    s += v;
                }
                return s;
            },
                us1,
                us2
            ).Test();

            us2.OnNext(1);

            to.AssertEmpty();

            us2.OnNext(2);

            to.AssertEmpty();

            us1.OnNext(10);

            to.AssertValuesOnly(11);

            us1.OnError(new InvalidOperationException());

            Assert.False(us2.HasObserver());

            to.AssertFailure(typeof(InvalidOperationException), 11);
        }

        [Test]
        public void Error_Second_Source()
        {
            var us1 = new UnicastSubject<int>();
            var us2 = new UnicastSubject<int>();

            var to = ReactiveExtensions.Zip(a =>
            {
                int s = 0;
                foreach (var v in a)
                {
                    s += v;
                }
                return s;
            },
                us1,
                us2
            ).Test();

            us1.OnNext(1);

            to.AssertEmpty();

            us1.OnNext(2);

            to.AssertEmpty();

            us2.OnNext(10);

            to.AssertValuesOnly(11);

            us2.OnError(new InvalidOperationException());

            Assert.False(us1.HasObserver());

            to.AssertFailure(typeof(InvalidOperationException), 11);
        }

        [Test]
        public void Error_Delayed()
        {
            var us1 = new UnicastSubject<int>();
            var us2 = new UnicastSubject<int>();

            var to = ReactiveExtensions.Zip(a =>
            {
                int s = 0;
                foreach (var v in a)
                {
                    s += v;
                }
                return s;
            }, true,
                us1,
                us2
            ).Test();

            us1.EmitError(new InvalidOperationException(), 1, 2, 3);

            Assert.True(us2.HasObserver(), "us2: No observers!");

            us2.Emit(10, 20, 30, 40);

            Assert.False(us2.HasObserver(), "us2: Observers present!");

            to.AssertFailure(typeof(InvalidOperationException), 11, 22, 33);
        }

        [Test]
        public void Mapper_Crashes()
        {
            var us1 = new UnicastSubject<int>();
            var us2 = new UnicastSubject<int>();

            var to = ReactiveExtensions.Zip(a =>
            {
                int s = 0;
                foreach (var v in a)
                {
                    s += v;
                }

                if (s == 33)
                {
                    throw new InvalidOperationException();
                }
                return s;
            },
                us1,
                us2
            ).Test();

            us1.Emit(1, 2, 3);

            Assert.True(us1.HasObserver());
            Assert.True(us2.HasObserver());

            us2.Emit(10, 20, 30, 40);

            Assert.False(us1.HasObserver());
            Assert.False(us2.HasObserver());

            to.AssertFailure(typeof(InvalidOperationException), 11, 22);
        }

        [Test]
        public void Race()
        {
            for (int i = 0; i < TestHelper.RACE_LOOPS; i++)
            {
                var us1 = new UnicastSubject<int>();
                var us2 = new UnicastSubject<int>();

                var to = new[] { us1, us2 }.Zip(a =>
                {
                    int v = 0;
                    foreach (var e in a)
                    {
                        v += e;
                    }
                    return v;
                }).Test();

                TestHelper.Race(() => {
                    for (int j = 0; j < 1000; j++)
                    {
                        us1.OnNext(j);
                    }
                    us1.OnCompleted();
                }, () => {
                    for (int j = 1000; j < 2000; j++)
                    {
                        us2.OnNext(j);
                    }
                    us2.OnCompleted();
                });

                to.AssertValueCount(1000)
                    .AssertNoError()
                    .AssertCompleted();

                var list = to.Items;
                for (int j = 0; j < 1000; j++)
                {
                    Assert.AreEqual(j + (1000 + j), list[j]);
                }
            }
        }

        [Test]
        public void Race_DelayErrors()
        {
            for (int i = 0; i < TestHelper.RACE_LOOPS; i++)
            {
                var us1 = new UnicastSubject<int>();
                var us2 = new UnicastSubject<int>();

                var to = new[] { us1, us2 }.Zip(a =>
                {
                    int v = 0;
                    foreach (var e in a)
                    {
                        v += e;
                    }
                    return v;
                }, true).Test();

                TestHelper.Race(() => {
                    for (int j = 0; j < 1000; j++)
                    {
                        us1.OnNext(j);
                    }
                    us1.OnCompleted();
                }, () => {
                    for (int j = 1000; j < 2000; j++)
                    {
                        us2.OnNext(j);
                    }
                    us2.OnCompleted();
                });

                to.AssertValueCount(1000)
                    .AssertNoError()
                    .AssertCompleted();

                var list = to.Items;
                for (int j = 0; j < 1000; j++)
                {
                    Assert.AreEqual(j + (1000 + j), list[j]);
                }
            }
        }
    }
}
