using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace akarnokd.reactive_extensions_test.maybe
{
    [TestFixture]
    public class MaybeRepeatTest
    {
        #region + Times +

        [Test]
        public void Times_Basic()
        {
            var count = 0;

            var src = MaybeSource.FromFunc(() => ++count);

            var obs = src.Repeat();

            obs
                .SubscribeOn(NewThreadScheduler.Default)
                .Take(5)
                .Test()
                .AwaitDone(TimeSpan.FromSeconds(5))
                .AssertResult(1, 2, 3, 4, 5);

            Assert.True(5 <= count, $"{count}");
        }

        [Test]
        public void Times_Empty()
        {
            var count = 0;

            var src = MaybeSource.FromAction<int>(() => {
                if (++count == 5)
                {
                    throw new InvalidOperationException();
                }
                
            });

            var obs = src.Repeat();

            obs
                .Test()
                .AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(5, count);
        }

        [Test]
        public void Times_Limit_Error()
        {
            var count = 0;

            var src = MaybeSource.FromAction<int>(() => {
                if (++count == 5)
                {
                    throw new InvalidOperationException();
                }

            });

            var obs = src.Repeat(5);

            obs
                .Test()
                .AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(5, count);
        }

        [Test]
        public void Times_Limit_Empty()
        {
            var count = 0;

            var src = MaybeSource.FromAction<int>(() => {
                if (++count == 6)
                {
                    throw new InvalidOperationException();
                }

            });

            var obs = src.Repeat(4);

            obs
                .Test()
                .AssertResult();

            Assert.AreEqual(5, count);
        }

        [Test]
        public void Times_Limit()
        {
            var count = 0;

            var src = MaybeSource.FromFunc(() => ++count);

            var obs = src.Repeat(4);

            obs
            .Test()
            .AssertResult(1, 2, 3, 4, 5);

            Assert.AreEqual(5, count);
        }

        [Test]
        public void Times_Error()
        {
            MaybeSource.Error<int>(new InvalidOperationException())
                .Repeat()
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Times_Dispose()
        {
            TestHelper.VerifyDisposeMaybe<int, int>(m => m.Repeat());
        }

        #endregion + Times +

        #region + Handler +


        [Test]
        public void Handler_Basic()
        {
            var count = 0;

            var src = MaybeSource.FromFunc(() => ++count);

            var obs = src.Repeat(v => true);

            obs
            .SubscribeOn(NewThreadScheduler.Default)
            .Take(5)
            .Test()
            .AwaitDone(TimeSpan.FromSeconds(5))
            .AssertResult(1, 2, 3, 4, 5);

            Assert.True(5 <= count, $"{count}");
        }

        [Test]
        public void Handler_Empty()
        {
            var count = 0;

            var src = MaybeSource.FromAction<int>(() => {
                if (++count == 5)
                {
                    throw new InvalidOperationException();
                }

            });

            var obs = src.Repeat(v => true);

            obs
                .Test()
                .AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(5, count);
        }

        [Test]
        public void Handler_Limit_Error()
        {
            var count = 0;

            var src = MaybeSource.FromAction<int>(() => {
                if (++count == 5)
                {
                    throw new InvalidOperationException();
                }

            });

            var obs = src.Repeat(v => v <= 5);

            obs
                .Test()
                .AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(5, count);
        }

        [Test]
        public void Handler_Limit_Empty()
        {
            var count = 0;

            var src = MaybeSource.FromAction<int>(() => {
                if (++count == 6)
                {
                    throw new InvalidOperationException();
                }

            });

            var obs = src.Repeat(v => v < 5);

            obs
                .Test()
                .AssertResult();

            Assert.AreEqual(5, count);
        }

        [Test]
        public void Handler_Limit()
        {
            var count = 0;

            var src = MaybeSource.FromFunc(() => ++count);

            var obs = src.Repeat(v => v < 5);

            obs
            .Test()
            .AssertResult(1, 2, 3, 4, 5);

            Assert.AreEqual(5, count);
        }

        [Test]
        public void Handler_Error()
        {
            MaybeSource.Error<int>(new InvalidOperationException())
                .Repeat(v => true)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Handler_Dispose()
        {
            TestHelper.VerifyDisposeMaybe<int, int>(m => m.Repeat(v => true));
        }

        [Test]
        public void Handler_False()
        {
            var count = 0;

            var src = MaybeSource.FromFunc(() => ++count);

            var obs = src.Repeat(v => false);

            obs
            .Test()
            .AssertResult(1);

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Handler_Empty_Crash()
        {
            var count = 0;

            var src = MaybeSource.FromAction<int>(() => ++count);

            var obs = src.Repeat(v => throw new InvalidOperationException());

            obs
            .Test()
            .AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(1, count);
        }

        #endregion + Handler +
    }
}
