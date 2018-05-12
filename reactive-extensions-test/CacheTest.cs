using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace akarnokd.reactive_extensions_test
{
    [TestFixture]
    public class CacheTest
    {
        [Test]
        public void Basic()
        {
            var count = 0;
            var v = Observable.Range(1, 5)
                 .DoOnSubscribe(() => count++)
                 .Cache();

            Assert.AreEqual(0, count);

            var to = v.Test();

            Assert.AreEqual(1, count);

            to.AssertResult(1, 2, 3, 4, 5);

            var to2 = v.Test();

            to2.AssertResult(1, 2, 3, 4, 5);
        }

        [Test]
        public void Error()
        {
            var o = Observable.Range(1, 5).ConcatError(new InvalidOperationException())
                .Cache();

            for (int i = 0; i < 3; i++)
            {
                o.Test().AssertFailure(typeof(InvalidOperationException), 1, 2, 3, 4, 5);
            }
        }

        [Test]
        public void Dispose()
        {
            var d = default(IDisposable);

            var us = new UnicastSubject<int>();

            var o = us.Cache(capacityHint: 16, cancel: v => d = v);

            Assert.IsNull(d);
            Assert.False(us.HasObserver());

            var to = o.Test();

            Assert.IsNotNull(d);
            Assert.True(us.HasObserver());

            d.Dispose();

            Assert.False(us.HasObserver());

            to.AssertFailure(typeof(OperationCanceledException));
        }

        [Test]
        public void Basic_Long()
        {
            var to = Observable.Range(1, 1000)
                .Cache()
                .Test()
                .AssertValueCount(1000)
                .AssertNoError()
                .AssertCompleted();

            var list = to.Items;
            for (int i = 1; i <= 1000; i++)
            {
                Assert.AreEqual(i, list[i - 1]);
            }
        }

        [Test]
        [Timeout(10000)]
        public void Race()
        {
            for (int i = 0; i < TestHelper.RACE_LOOPS; i++)
            {
                var subj = new Subject<int>();

                var o = subj.Cache();

                var to1 = o.Test();

                var to2 = new TestObserver<int>();

                TestHelper.Race(
                    () =>
                    {
                        for (int j = 0; j < 1000; j++)
                        {
                            subj.OnNext(j);
                        }
                        subj.OnCompleted();
                    },
                    () =>
                    {
                        while (to1.ItemCount < 250) ;
                        o.Subscribe(to2);
                    }
                );

                to1.AssertValueCount(1000)
                .AssertNoError()
                .AssertCompleted();

                var list = to1.Items;
                for (int j = 0; j < 1000; j++)
                {
                    Assert.AreEqual(j, list[j]);
                }

                to2
                .AwaitDone(TimeSpan.FromSeconds(5))
                .AssertValueCount(1000)
                .AssertNoError()
                .AssertCompleted();

                list = to2.Items;
                for (int j = 0; j < 1000; j++)
                {
                    Assert.AreEqual(j, list[j]);
                }

            }
        }
    }
}
