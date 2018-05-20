using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Reactive.Concurrency;
using System.Threading;

namespace akarnokd.reactive_extensions_test.maybe
{
    [TestFixture]
    public class MaybeUnsubscribeOnTest
    {
        [Test]
        public void Basic()
        {
            var name = "";

            MaybeSource.Empty<int>()
                .DoOnDispose(() => name = Thread.CurrentThread.Name)
                .UnsubscribeOn(NewThreadScheduler.Default)
                .Test()
                .AssertResult();

            Assert.AreEqual("", name);
        }

        [Test]
        public void Success()
        {
            var name = "";

            MaybeSource.Just(1)
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

            MaybeSource.Empty<int>()
                .DoOnDispose(() => name = Thread.CurrentThread.Name)
                .UnsubscribeOn(NewThreadScheduler.Default)
                .Test()
                .AssertResult();

            Assert.AreEqual("", name);
        }

        [Test]
        public void Dispose()
        {
            var name = "";
            var cdl = new CountdownEvent(1);

            MaybeSource.Never<int>()
                .DoOnDispose(() =>
                {
                    name = Thread.CurrentThread.Name;
                    cdl.Signal();
                })
                .UnsubscribeOn(NewThreadScheduler.Default)
                .Test()
                .Dispose();

            Assert.True(cdl.Wait(5000));

            Assert.AreNotEqual("", name);
            Assert.AreNotEqual(Thread.CurrentThread.Name, name);
        }


    }
}
