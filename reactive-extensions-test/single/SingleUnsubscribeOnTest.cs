using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Reactive.Concurrency;
using System.Threading;

namespace akarnokd.reactive_extensions_test.single
{
    [TestFixture]
    public class SingleUnsubscribeOnTest
    {
        [Test]
        public void Success()
        {
            var name = "";

            SingleSource.Just(1)
                .DoOnDispose(() => name = Thread.CurrentThread.Name)
                .UnsubscribeOn(NewThreadScheduler.Default)
                .Test()
                .AssertResult(1);

            Assert.AreEqual("", name);
        }

        [Test]
        public void Error()
        {
            var name = "";

            SingleSource.Error<int>(new InvalidOperationException())
                .DoOnDispose(() => name = Thread.CurrentThread.Name)
                .UnsubscribeOn(NewThreadScheduler.Default)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual("", name);
        }

        [Test]
        public void Dispose()
        {
            var name = -1;
            var cdl = new CountdownEvent(1);

            SingleSource.Never<int>()
                .DoOnDispose(() =>
                {
                    name = Thread.CurrentThread.ManagedThreadId;
                    cdl.Signal();
                })
                .UnsubscribeOn(NewThreadScheduler.Default)
                .Test()
                .Dispose();

            Assert.True(cdl.Wait(5000));

            Assert.AreNotEqual(-1, name);
            Assert.AreNotEqual(Thread.CurrentThread.ManagedThreadId, name);
        }


    }
}
