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
            var name = -1;

            CompletableSource.FromAction(() =>
            {
                name = Thread.CurrentThread.ManagedThreadId;
            })
            .SubscribeOn(NewThreadScheduler.Default)
            .Test()
            .AwaitDone(TimeSpan.FromSeconds(5))
            .AssertResult();

            Assert.AreNotEqual(-1, name);
            Assert.AreNotEqual(Thread.CurrentThread.ManagedThreadId, name);
        }

        [Test]
        public void Error()
        {
            var name = -1;

            CompletableSource.FromAction(() =>
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
