using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Reactive.Concurrency;
using System.Threading;

namespace akarnokd.reactive_extensions_test.single
{
    [TestFixture]
    public class SingleObserveOnTest
    {

        [Test]
        public void Success()
        {
            var name = -1;

            SingleSource.Just(1)
                .ObserveOn(NewThreadScheduler.Default)
                .DoOnSuccess(v => name = Thread.CurrentThread.ManagedThreadId)
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

            SingleSource.Error<int>(new InvalidOperationException())
                .ObserveOn(NewThreadScheduler.Default)
                .DoOnError(e => name = Thread.CurrentThread.ManagedThreadId)
                .Test()
                .AwaitDone(TimeSpan.FromSeconds(5))
                .AssertFailure(typeof(InvalidOperationException));

            Assert.AreNotEqual(-1, name);
            Assert.AreNotEqual(Thread.CurrentThread.ManagedThreadId, name);
        }

        [Test]
        public void Dispose()
        {
            var cs = new SingleSubject<int>();

            cs.ObserveOn(NewThreadScheduler.Default)
                .Test(true)
                .AssertEmpty();

            Assert.False(cs.HasObserver());
        }
    }
}
