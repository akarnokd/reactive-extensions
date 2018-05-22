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
            var name = "";

            SingleSource.Just(1)
                .ObserveOn(NewThreadScheduler.Default)
                .DoOnSuccess(v => name = Thread.CurrentThread.Name)
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

            SingleSource.Error<int>(new InvalidOperationException())
                .ObserveOn(NewThreadScheduler.Default)
                .DoOnError(e => name = Thread.CurrentThread.Name)
                .Test()
                .AwaitDone(TimeSpan.FromSeconds(5))
                .AssertFailure(typeof(InvalidOperationException));

            Assert.AreNotEqual("", name);
            Assert.AreNotEqual(Thread.CurrentThread.Name, name);
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
