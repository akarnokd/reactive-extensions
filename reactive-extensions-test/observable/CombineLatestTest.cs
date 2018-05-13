using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.observable
{
    [TestFixture]
    public class CombineLatestTest
    {
        [Test]
        public void Basic()
        {
            var us1 = new UnicastSubject<int>();
            var us2 = new UnicastSubject<int>();

            var to = ReactiveExtensions.CombineLatest(a =>
            {
                int v = 0;
                foreach (var e in a)
                {
                    v += e;
                }
                return v;
            }, us1, us2).Test();

            to.AssertEmpty();

            us1.OnNext(1);

            to.AssertEmpty();

            us1.OnNext(2);

            to.AssertEmpty();

            us2.OnNext(10);

            to.AssertValuesOnly(12);

            us2.OnNext(20);

            to.AssertValuesOnly(12, 22);

            us2.OnCompleted();

            to.AssertValuesOnly(12, 22);

            us1.OnNext(3);

            to.AssertValuesOnly(12, 22, 23);

            us1.OnCompleted();

            to.AssertResult(12, 22, 23);
        }

        [Test]
        public void No_Combinations_Complete_After_All()
        {
            var us1 = new UnicastSubject<int>();
            var us2 = new UnicastSubject<int>();

            var to = ReactiveExtensions.CombineLatest(a =>
            {
                int v = 0;
                foreach (var e in a)
                {
                    v += e;
                }
                return v;
            }, us1, us2).Test();

            to.AssertEmpty();

            us1.OnCompleted();

            Assert.True(us2.HasObserver(), "Other source disposed?");

            us2.EmitAll(1, 2, 3, 4, 5);

            to.AssertResult();
        }

        [Test]
        public void Error()
        {
            var us1 = new UnicastSubject<int>();
            var us2 = new UnicastSubject<int>();

            var to = ReactiveExtensions.CombineLatest(a =>
            {
                int v = 0;
                foreach (var e in a)
                {
                    v += e;
                }
                return v;
            }, us1, us2).Test();

            to.AssertEmpty();

            us1.OnError(new InvalidOperationException());

            Assert.False(us2.HasObserver());

            to.AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Error_Delayed()
        {
            var us1 = new UnicastSubject<int>();
            var us2 = new UnicastSubject<int>();

            var to = ReactiveExtensions.CombineLatest(a =>
            {
                int v = 0;
                foreach (var e in a)
                {
                    v += e;
                }
                return v;
            }, true, us1, us2).Test();

            to.AssertEmpty();

            us1.OnNext(1);
            us1.OnError(new InvalidOperationException());

            Assert.True(us2.HasObserver());

            us2.EmitAll(10, 20, 30, 40, 50);

            to.AssertFailure(typeof(InvalidOperationException), 11, 21, 31, 41, 51);
        }

        [Test]
        public void Race()
        {
            for (int i = 0; i < TestHelper.RACE_LOOPS; i++)
            {
                var us1 = new UnicastSubject<int>();
                var us2 = new UnicastSubject<int>();

                var to = new[] { us1, us2 }.CombineLatest(a =>
                {
                    int v = 0;
                    foreach (var e in a)
                    {
                        v += e;
                    }
                    return v;
                }).Test();

                us1.OnNext(0);
                us2.OnNext(1000);

                TestHelper.Race(() => {
                    for (int j = 1; j < 1000; j++)
                    {
                        us1.OnNext(j);
                    }
                    us1.OnCompleted();
                }, () => {
                    for (int j = 1001; j < 2000; j++)
                    {
                        us2.OnNext(j);
                    }
                    us2.OnCompleted();
                });

                to.AssertValueCount(1999)
                    .AssertNoError()
                    .AssertCompleted();
            }
        }

        [Test]
        public void Race_DelayErrors()
        {
            for (int i = 0; i < TestHelper.RACE_LOOPS; i++)
            {
                var us1 = new UnicastSubject<int>();
                var us2 = new UnicastSubject<int>();

                var to = new[] { us1, us2 }.CombineLatest(a =>
                {
                    int v = 0;
                    foreach (var e in a)
                    {
                        v += e;
                    }
                    return v;
                }, true).Test();

                us1.OnNext(0);
                us2.OnNext(1000);

                TestHelper.Race(() => {
                    for (int j = 1; j < 1000; j++)
                    {
                        us1.OnNext(j);
                    }
                    us1.OnCompleted();
                }, () => {
                    for (int j = 1001; j < 2000; j++)
                    {
                        us2.OnNext(j);
                    }
                    us2.OnCompleted();
                });

                to.AssertValueCount(1999)
                    .AssertNoError()
                    .AssertCompleted();
            }
        }

        [Test]
        public void Mapper_Crash()
        {
            var us1 = new UnicastSubject<int>();
            var us2 = new UnicastSubject<int>();

            var to = ReactiveExtensions.CombineLatest(a =>
            {
                int v = 0;
                foreach (var e in a)
                {
                    v += e;
                }
                if (v == 22)
                {
                    throw new InvalidOperationException();
                }
                return v;
            }, us1, us2).Test();

            to.AssertEmpty();

            us1.OnNext(1);
            us2.OnNext(10);

            us1.OnNext(2);
            us2.OnNext(20);

            Assert.False(us1.HasObserver());
            Assert.False(us2.HasObserver());

            to.AssertFailure(typeof(InvalidOperationException), 11, 12);
        }
    }
}
