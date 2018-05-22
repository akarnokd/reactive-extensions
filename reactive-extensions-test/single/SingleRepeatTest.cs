using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace akarnokd.reactive_extensions_test.single
{
    [TestFixture]
    public class SingleRepeatTest
    {
        #region + Times +

        [Test]
        public void Times_Basic()
        {
            var count = 0;

            var src = SingleSource.FromFunc(() => ++count);

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
        public void Times_Limit()
        {
            var count = 0;

            var src = SingleSource.FromFunc(() => ++count);

            var obs = src.Repeat(4);

            obs
            .Test()
            .AssertResult(1, 2, 3, 4, 5);

            Assert.AreEqual(5, count);
        }

        [Test]
        public void Times_Error()
        {
            SingleSource.Error<int>(new InvalidOperationException())
                .Repeat()
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Times_Dispose()
        {
            TestHelper.VerifyDisposeSingle<int, int>(m => m.Repeat());
        }

        #endregion + Times +

        #region + Handler +


        [Test]
        public void Handler_Basic()
        {
            var count = 0;

            var src = SingleSource.FromFunc(() => ++count);

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
        public void Handler_Limit()
        {
            var count = 0;

            var src = SingleSource.FromFunc(() => ++count);

            var obs = src.Repeat(v => v < 5);

            obs
            .Test()
            .AssertResult(1, 2, 3, 4, 5);

            Assert.AreEqual(5, count);
        }

        [Test]
        public void Handler_Error()
        {
            SingleSource.Error<int>(new InvalidOperationException())
                .Repeat(v => true)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Handler_Dispose()
        {
            TestHelper.VerifyDisposeSingle<int, int>(m => m.Repeat(v => true));
        }

        [Test]
        public void Handler_False()
        {
            var count = 0;

            var src = SingleSource.FromFunc(() => ++count);

            var obs = src.Repeat(v => false);

            obs
            .Test()
            .AssertResult(1);

            Assert.AreEqual(1, count);
        }

        #endregion + Handler +
    }
}
