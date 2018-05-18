using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Threading;
using System.Reactive.Concurrency;

namespace akarnokd.reactive_extensions_test.completable
{
    [TestFixture]
    public class CompletableSubscribeOnTest
    {
        [Test]
        public void Basic()
        {
            var name = "";

            CompletableSource.FromAction(() =>
            {
                name = Thread.CurrentThread.Name;
            })
            .SubscribeOn(NewThreadScheduler.Default)
            .Test()
            .AwaitDone(TimeSpan.FromSeconds(5))
            .AssertResult();

            Assert.AreNotEqual("", name);
            Assert.AreNotEqual(Thread.CurrentThread.Name, name);
        }

        [Test]
        public void Error()
        {
            var name = "";

            CompletableSource.FromAction(() =>
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

            CompletableSource.FromAction(() =>
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
