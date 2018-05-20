using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Threading;
using System.Reactive.Concurrency;

namespace akarnokd.reactive_extensions_test.maybe
{
    [TestFixture]
    public class MaybeSubscribeOnTest
    {
        [Test]
        public void Basic()
        {
            var name = "";

            MaybeSource.FromAction<int>(() =>
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
        public void Success()
        {
            var name = "";

            MaybeSource.FromFunc(() =>
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

            MaybeSource.FromAction<int>(() =>
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

            MaybeSource.FromAction<int>(() =>
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
