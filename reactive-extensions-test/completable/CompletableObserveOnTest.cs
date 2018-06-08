using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Reactive.Concurrency;
using System.Threading;

namespace akarnokd.reactive_extensions_test.completable
{
    [TestFixture]
    public class CompletableObserveOnTest
    {
        [Test]
        public void Basic()
        {
            var name = -1;

            CompletableSource.Empty()
                .ObserveOn(NewThreadScheduler.Default)
                .DoOnCompleted(() => name = Thread.CurrentThread.ManagedThreadId)
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

            CompletableSource.Error(new InvalidOperationException())
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
            var cs = new CompletableSubject();

            cs.ObserveOn(NewThreadScheduler.Default)
                .Test(true)
                .AssertEmpty();

            Assert.False(cs.HasObserver());
        }

        [Test]
        public void Race_Complete_Dispose()
        {
            for (int i = 0; i < TestHelper.RACE_LOOPS; i++)
            {
                var cs = new CompletableSubject();

                var to = cs.ObserveOn(NewThreadScheduler.Default)
                    .Test();

                TestHelper.Race(() => {
                    cs.OnCompleted();
                }, () => {
                    to.Dispose();
                });
            }
        }
    }
}
