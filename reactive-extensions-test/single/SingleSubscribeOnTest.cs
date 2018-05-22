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
            var name = "";

            SingleSource.FromFunc(() =>
            {
                name = Thread.CurrentThread.Name;
                return 1;
            })
            .SubscribeOn(NewThreadScheduler.Default)
            .Test()
            .AwaitDone(TimeSpan.FromSeconds(5))
            .AssertResult(1);

            Assert.AreNotEqual("", name);
            Assert.AreNotEqual(Thread.CurrentThread.Name, name);
        }

        [Test]
        public void Error()
        {
            var name = "";

            SingleSource.FromFunc<int>(() =>
            {
                name = Thread.CurrentThread.Name;
                throw new InvalidOperationException();
            })
            .SubscribeOn(NewThreadScheduler.Default)
            .Test()
            .AwaitDone(TimeSpan.FromSeconds(5))
            .AssertFailure(typeof(InvalidOperationException));

            Assert.AreNotEqual("", name);
            Assert.AreNotEqual(Thread.CurrentThread.Name, name);
        }

        [Test]
        public void DisposeUpfront()
        {
            var name = "";

            SingleSource.FromFunc<int>(() =>
            {
                name = Thread.CurrentThread.Name;
                throw new InvalidOperationException();
            })
            .SubscribeOn(NewThreadScheduler.Default)
            .Test(true)
            .AssertEmpty();

            Assert.AreEqual("", name);
        }
    }
}
