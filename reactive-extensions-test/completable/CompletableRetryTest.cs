using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.completable
{
    [TestFixture]
    public class CompletableRetryTest
    {
        [Test]
        public void Basic()
        {
            CompletableSource.Empty()
                .Retry()
                .Test()
                .AssertResult();
        }

        [Test]
        public void Basic_Finite()
        {
            CompletableSource.Empty()
                .Retry(5)
                .Test()
                .AssertResult();
        }

        [Test]
        public void Error()
        {
            for (int i = 0; i < 100; i++)
            {
                var count = 0;

                CompletableSource.FromAction(() =>
                {
                    count++;
                    throw new InvalidOperationException("" + count);
                })
                .Retry(i)
                .Test()
                .AssertFailure(typeof(InvalidOperationException))
                .AssertError(typeof(InvalidOperationException), "" + (i + 1));
            }
        }

        [Test]
        public void Error_Infinite()
        {
            var count = 0;

            CompletableSource.FromAction(() =>
            {
                if (count++ < 5)
                {
                    throw new InvalidOperationException("" + count);
                }
            })
            .Retry()
            .Test()
            .AssertResult();

            Assert.AreEqual(6, count);
        }

        [Test]
        [Timeout(10000)]
        public void Dispose()
        {
            var count = 0;
            var us = new CompletableSubject();

            var to = CompletableSource.Defer(() =>
            {
                if (count++ < 5)
                {
                    return CompletableSource.Error(new InvalidOperationException());
                }
                return us;
            })
            .Retry()
            .Test();

            to.AssertEmpty();

            Assert.True(us.HasObserver());

            to.Dispose();

            Assert.False(us.HasObserver());

            Assert.AreEqual(6, count);
        }

        [Test]
        public void Predicate()
        {
            CompletableSource.Empty()
                .Retry((e, c) => true)
                .Test()
                .AssertResult();
        }

        [Test]
        public void Predicate_Finite()
        {
            CompletableSource.Empty()
                .Retry((e, c) => true)
                .Test()
                .AssertResult();
        }

        [Test]
        public void Predicate_Error()
        {
            for (int i = 0; i < 100; i++)
            {
                var count = 0;

                CompletableSource.FromAction(() =>
                {
                    count++;
                    throw new InvalidOperationException("" + count);
                })
                .Retry((e, c) => c <= i)
                .Test()
                .WithTag("i=" + i)
                .AssertFailure(typeof(InvalidOperationException))
                .AssertError(typeof(InvalidOperationException), "" + (i + 1));
            }
        }

        [Test]
        public void Predicate_Infinite()
        {
            var count = 0;

            CompletableSource.FromAction(() =>
            {
                if (count++ < 5)
                {
                    throw new InvalidOperationException("" + count);
                }
            })
            .Retry((e, c) => true)
            .Test()
            .AssertResult();

            Assert.AreEqual(6, count);
        }

        [Test]
        [Timeout(10000)]
        public void Predicate_Dispose()
        {
            var count = 0;
            var us = new CompletableSubject();

            var to = CompletableSource.Defer(() =>
            {
                if (count++ < 5)
                {
                    return CompletableSource.Error(new InvalidOperationException());
                }
                return us;
            })
            .Retry((e, c) => true)
            .Test();

            to.AssertEmpty();

            Assert.True(us.HasObserver());

            to.Dispose();

            Assert.False(us.HasObserver());

            Assert.AreEqual(6, count);
        }

        [Test]
        public void Predicate_Wrong_Error()
        {
            CompletableSource.Error(new InvalidOperationException())
                .Retry((e, i) => typeof(NotImplementedException).IsAssignableFrom(e.GetType()))
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Predicate_Crash()
        {
            CompletableSource.Error(new InvalidOperationException("outer"))
                .Retry((e, i) => {
                    throw new InvalidOperationException("inner");
                })
                .Test()
                .AssertFailure(typeof(AggregateException))
                .AssertCompositeError(0, typeof(InvalidOperationException), "outer")
                .AssertCompositeError(1, typeof(InvalidOperationException), "inner")
                ;

        }
    }
}
