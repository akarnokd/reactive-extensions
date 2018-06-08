using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Threading;
using System.Reactive.Concurrency;

namespace akarnokd.reactive_extensions_test.single
{
    [TestFixture]
    public class SingleSubscribeOnTest
    {
        [Test]
        public void Success()
        {
            var name = -1;

            SingleSource.FromFunc(() =>
            {
                name = Thread.CurrentThread.ManagedThreadId;
                return 1;
            })
            .SubscribeOn(NewThreadScheduler.Default)
            .Test()
            .AwaitDone(TimeSpan.FromSeconds(5))
            .AssertResult(1);

            Assert.AreNotEqual(-1, name);
            Assert.AreNotEqual(Thread.CurrentThread.ManagedThreadId, name);
        }

        [Test]
        public void Error()
        {
            var name = -1;

            SingleSource.FromFunc<int>(() =>
            {
                name = Thread.CurrentThread.ManagedThreadId;
                throw new InvalidOperationException();
            })
            .SubscribeOn(NewThreadScheduler.Default)
            .Test()
            .AwaitDone(TimeSpan.FromSeconds(5))
            .AssertFailure(typeof(InvalidOperationException));

            Assert.AreNotEqual(-1, name);
            Assert.AreNotEqual(Thread.CurrentThread.ManagedThreadId, name);
        }

        [Test]
        public void DisposeUpfront()
        {
            var name = -1;

            SingleSource.FromFunc<int>(() =>
            {
                name = Thread.CurrentThread.ManagedThreadId;
                throw new InvalidOperationException();
            })
            .SubscribeOn(NewThreadScheduler.Default)
            .Test(true)
            .AssertEmpty();

            Assert.AreEqual(-1, name);
        }
    }
}
