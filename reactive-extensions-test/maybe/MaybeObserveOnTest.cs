using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Reactive.Concurrency;
using System.Threading;

namespace akarnokd.reactive_extensions_test.maybe
{
    [TestFixture]
    public class MaybeObserveOnTest
    {
        [Test]
        public void Basic()
        {
            var name = "";

            MaybeSource.Empty<int>()
                .ObserveOn(NewThreadScheduler.Default)
                .DoOnCompleted(() => name = Thread.CurrentThread.Name)
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

            MaybeSource.Just(1)
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

            MaybeSource.Error<int>(new InvalidOperationException())
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
            var cs = new MaybeSubject<int>();

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
                var cs = new MaybeSubject<int>();

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
