using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Subjects;

namespace akarnokd.reactive_extensions_test
{
    [TestFixture]
    public class BlockingEnumerableTest
    {
        [Test]
        public void Basic_Range()
        {
            var to = new TestObserver<int>();

            foreach (var v in Observable.Range(1, 5).BlockingEnumerable())
            {
                to.OnNext(v);
            }
            to.OnCompleted();

            to.AssertResult(1, 2, 3, 4, 5);
        }

        [Test]
        public void Basic_Error()
        {
            var to = new TestObserver<int>();

            try
            {
                foreach (var v in Observable.Range(1, 5)
                    .ConcatError(new InvalidOperationException())
                    .BlockingEnumerable())
                {
                    to.OnNext(v);
                }

            } catch (Exception ex)
            {
                to.OnError(ex);
            }

            to.AssertFailure(typeof(InvalidOperationException), 1, 2, 3, 4, 5);
        }

        [Test]
        public void Dispose()
        {
            var us = new UnicastSubject<int>();

            var en = us.BlockingEnumerable().GetEnumerator();

            Assert.True(us.HasObserver());

            en.Dispose();

            Assert.False(us.HasObserver());

            Assert.False(en.MoveNext());
        }

        [Test]
        public void Race()
        {
            for (int i = 0; i < TestHelper.RACE_LOOPS; i++)
            {
                var us = new UnicastSubject<int>();

                var to = new TestObserver<int>();

                TestHelper.Race(() =>
                {
                    for (int j = 0; j < 1000; j++)
                    {
                        us.OnNext(j);
                    }
                    us.OnCompleted();
                },
                () =>
                {
                    foreach (var v in us.BlockingEnumerable())
                    {
                        to.OnNext(v);
                    }
                    to.OnCompleted();
                });

                to.AssertValueCount(1000)
                    .AssertNoError()
                    .AssertCompleted();
            }
        }

        [Test]
        public void Race_With_Error()
        {
            var exc = new InvalidOperationException();

            for (int i = 0; i < TestHelper.RACE_LOOPS; i++)
            {
                var us = new UnicastSubject<int>();

                var to = new TestObserver<int>();

                TestHelper.Race(() =>
                {
                    for (int j = 0; j < 1000; j++)
                    {
                        us.OnNext(j);
                    }
                    us.OnError(exc);
                },
                () =>
                {
                    try
                    {
                        foreach (var v in us.BlockingEnumerable())
                        {
                            to.OnNext(v);
                        }
                    } catch (Exception ex)
                    {
                        to.OnError(ex);
                    }
                });

                to.AssertValueCount(1000)
                    .AssertError(typeof(InvalidOperationException))
                    .AssertNotCompleted();
            }
        }
    }
}
