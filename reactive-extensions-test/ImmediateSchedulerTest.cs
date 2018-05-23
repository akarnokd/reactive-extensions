using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test
{
    [TestFixture]
    public class ImmediateScheduleTest
    {
        [Test]
        public void Direct()
        {
            var count = 0;
            ImmediateScheduler.INSTANCE.Schedule(1, (s, t) => { ++count; return DisposableHelper.EMPTY; });

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Delayed()
        {
            var count = 0;
            ImmediateScheduler.INSTANCE.Schedule(1, TimeSpan.FromMilliseconds(100), (s, t) => { ++count; return DisposableHelper.EMPTY; });

            Assert.AreEqual(1, count);
        }

        [Test]
        public void DueDate()
        {
            var count = 0;
            ImmediateScheduler.INSTANCE.Schedule(1, DateTimeOffset.Now + TimeSpan.FromMilliseconds(100), (s, t) => { ++count; return DisposableHelper.EMPTY; });

            Assert.AreEqual(1, count);
        }
    }
}
