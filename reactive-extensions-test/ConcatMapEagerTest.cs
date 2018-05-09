using System;
using NUnit.Framework;
using System.Reactive.Linq;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test
{
    [TestFixture]
    public class ConcatMapEagerTest
    {
        [Test]
        public void Basic_All()
        {
            Observable.Range(1, 5)
                 .ConcatMapEager(v => Observable.Range(v * 100, 5))
                 .Test()
                 .AssertResult(
                     100, 101, 102, 103, 104,
                     200, 201, 202, 203, 204,
                     300, 301, 302, 303, 304,
                     400, 401, 402, 403, 404,
                     500, 501, 502, 503, 504
                 );
        }

        [Test]
        public void Basic_Max_Concurrent()
        {
            Observable.Range(1, 5)
                 .ConcatMapEager(v => Observable.Range(v, 5), 2)
                 .Test()
                 .AssertResult(
                     1, 2, 3, 4, 5,
                     2, 3, 4, 5, 6,
                     3, 4, 5, 6, 7,
                     4, 5, 6, 7, 8,
                     5, 6, 7, 8, 9
                 );
        }

        [Test]
        public void Error_Main()
        {
            Observable.Throw<int>(new InvalidOperationException())
                 .ConcatMapEager(v => Observable.Range(v, 5))
                 .Test()
                 .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Error_Inner()
        {
            Observable.Range(1, 5)
                 .ConcatMapEager(v => Observable.Throw<int>(new InvalidOperationException()))
                 .Test()
                 .AssertFailure(typeof(InvalidOperationException));
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
                .ConcatMapEager(v => us[v], 1)
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
                .ConcatMapEager(v => us[v], 2)
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
        public void Race_Max_Concurrency()
        {
            for (int i = 0; i < TestHelper.RACE_LOOPS; i++)
            {
                var us = new UnicastSubject<int>[]
                {
                    new UnicastSubject<int>(),
                    new UnicastSubject<int>(),
                };

                var to = Observable.Range(0, 2)
                    .ConcatMapEager(v => us[v])
                    .Test();

                Action a1 = () =>
                {
                    for (int j = 0; j < 1000; j++)
                    {
                        us[0].OnNext(j);
                    }
                    us[0].OnCompleted();
                };

                Action a2 = () =>
                {
                    for (int j = 0; j < 1000; j++)
                    {
                        us[1].OnNext(j);
                    }
                    us[1].OnCompleted();
                };

                TestHelper.Race(a1, a2);

                to.AssertValueCount(2000)
                    .AssertNoError()
                    .AssertCompleted();
            }
        }
        [Test]
        public void Race_Concurrency_1()
        {
            for (int i = 0; i < TestHelper.RACE_LOOPS; i++)
            {
                var us = new UnicastSubject<int>[]
                {
                    new UnicastSubject<int>(),
                    new UnicastSubject<int>(),
                };

                var to = Observable.Range(0, 2)
                    .ConcatMapEager(v => us[v], 1)
                    .Test();

                Action a1 = () =>
                {
                    for (int j = 0; j < 1000; j++)
                    {
                        us[0].OnNext(j);
                    }
                    us[0].OnCompleted();
                };

                Action a2 = () =>
                {
                    for (int j = 0; j < 1000; j++)
                    {
                        us[1].OnNext(j);
                    }
                    us[1].OnCompleted();
                };

                TestHelper.Race(a1, a2);

                to.AssertValueCount(2000)
                    .AssertNoError()
                    .AssertCompleted();
            }
        }
    }
}