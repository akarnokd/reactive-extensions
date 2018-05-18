using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.completable
{
    [TestFixture]
    public class CompletableRepeatTest
    {
        [Test]
        [Timeout(10000)]
        public void Basic_Finite()
        {
            for (int i = 0; i < 100; i++)
            {
                var count = 0;

                CompletableSource.FromAction(() => count++)
                    .Repeat(i)
                    .Test()
                    .AssertResult();

                Assert.AreEqual(i + 1, count);
            }
        }

        [Test]
        [Timeout(10000)]
        public void Basic_Infinite()
        {
            var count = 0;

            CompletableSource.FromAction(() => {
                if (++count == 100)
                {
                    throw new InvalidOperationException();
                }
            })
                .Repeat()
                .Test()
                .AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(100, count);
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
                    return CompletableSource.Empty();
                }
                return us;
            })
            .Repeat()
            .Test();

            to.AssertEmpty();

            Assert.True(us.HasObserver());

            to.Dispose();

            Assert.False(us.HasObserver());

            Assert.AreEqual(6, count);
        }

        [Test]
        [Timeout(10000)]
        public void Predicate_Finite()
        {
            for (int i = 0; i < 100; i++)
            {
                var count = 0;

                CompletableSource.FromAction(() => count++)
                    .Repeat(times => times <= i)
                    .Test()
                    .AssertResult();

                Assert.AreEqual(i + 1, count);
            }
        }

        [Test]
        [Timeout(10000)]
        public void Predicate_Infinite()
        {
            var count = 0;

            CompletableSource.FromAction(() => {
                if (++count == 100)
                {
                    throw new InvalidOperationException();
                }
            })
                .Repeat(times => true)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(100, count);
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
                    return CompletableSource.Empty();
                }
                return us;
            })
            .Repeat(times => true)
            .Test();

            to.AssertEmpty();

            Assert.True(us.HasObserver());

            to.Dispose();

            Assert.False(us.HasObserver());

            Assert.AreEqual(6, count);
        }

        [Test]
        [Timeout(10000)]
        public void Predicate_Crash()
        {
            CompletableSource.Empty()
            .Repeat(times => { throw new InvalidOperationException(); })
            .Test()
            .AssertFailure(typeof(InvalidOperationException));
        }
    }
}
