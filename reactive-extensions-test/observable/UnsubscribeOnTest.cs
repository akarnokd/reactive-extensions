using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Threading;
using System.Reactive.Concurrency;

namespace akarnokd.reactive_extensions_test.observable
{
    [TestFixture]
    public class UnsubscribeOn
    {
        [Test]
        public void Dispose()
        {
            var us = new UnicastSubject<int>();

            var n0 = Thread.CurrentThread.ManagedThreadId;

            var cdl = new CountdownEvent(1);

            var n1 = default(int);

            var to = us.DoOnDispose(() =>
            {
                n1 = Thread.CurrentThread.ManagedThreadId;
                cdl.Signal();
            })
            .UnsubscribeOn(NewThreadScheduler.Default)
            .Test();

            us.OnNext(1);

            to.AssertValuesOnly(1);

            to.Dispose();

            Assert.True(cdl.Wait(TimeSpan.FromSeconds(5)));

            Assert.AreNotEqual(n0, n1);
        }

        [Test]
        public void Complete()
        {
            var us = new UnicastSubject<int>();

            var n0 = Thread.CurrentThread.ManagedThreadId;

            var cdl = new CountdownEvent(1);

            var n1 = default(int);

            var to = us.DoOnDispose(() =>
            {
                n1 = Thread.CurrentThread.ManagedThreadId;
                cdl.Signal();
            })
            .UnsubscribeOn(NewThreadScheduler.Default)
            .Test();

            us.OnNext(1);

            us.OnCompleted();

            to.AssertResult(1);

            Assert.True(cdl.Wait(TimeSpan.FromSeconds(5)));

            Assert.AreNotEqual(n0, n1);
        }

        [Test]
        public void Error()
        {
            var us = new UnicastSubject<int>();

            var n0 = Thread.CurrentThread.ManagedThreadId;

            var cdl = new CountdownEvent(1);

            var n1 = default(int);

            var to = us.DoOnDispose(() =>
            {
                n1 = Thread.CurrentThread.ManagedThreadId;
                cdl.Signal();
            })
            .UnsubscribeOn(NewThreadScheduler.Default)
            .Test();

            us.OnNext(1);

            us.OnError(new InvalidOperationException());

            to.AssertFailure(typeof(InvalidOperationException), 1);

            Assert.True(cdl.Wait(TimeSpan.FromSeconds(5)));

            Assert.AreNotEqual(n0, n1);
        }
    }
}
