using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Reactive.Concurrency;
using System.Threading;

namespace akarnokd.reactive_extensions_test.observablesource
{
    [TestFixture]
    public class ObservableSourceSubscribeOnTest
    {
        [Test]
        public void Basic()
        {
            ObservableSource.Range(1, 5)
                .SubscribeOn(ThreadPoolScheduler.Instance)
                .Test()
                .AwaitDone(TimeSpan.FromSeconds(5))
                .AssertResult(1, 2, 3, 4, 5);
        }

        [Test]
        public void Different_Thread()
        {
            var name = Thread.CurrentThread.Name;

            var nameOn = "";

            ObservableSource.FromFunc<int>(() =>
            {
                nameOn = Thread.CurrentThread.Name;
                return 1;
            })
                .SubscribeOn(ThreadPoolScheduler.Instance)
                .Test()
                .AwaitDone(TimeSpan.FromSeconds(5))
                .AssertResult(1);

            Assert.AreNotEqual(name, nameOn);
        }

        [Test]
        public void Different_Thread_Error()
        {
            var name = Thread.CurrentThread.Name;

            var nameOn = "";

            ObservableSource.FromFunc<int>(() =>
            {
                nameOn = Thread.CurrentThread.Name;
                throw new InvalidOperationException();
            })
                .SubscribeOn(ThreadPoolScheduler.Instance)
                .Test()
                .AwaitDone(TimeSpan.FromSeconds(5))
                .AssertFailure(typeof(InvalidOperationException));

            Assert.AreNotEqual(name, nameOn);
        }

        [Test]
        public void Dispose()
        {
            var subj = new PublishSubject<int>();

            var to = subj.SubscribeOn(ThreadPoolScheduler.Instance)
                .Test();

            int cnt = 0;
            while (!subj.HasObservers)
            {
                if (++cnt == 5000)
                {
                    Assert.Fail("No observers showed up?!");
                }
                Thread.Sleep(1);
            }

            subj.OnNext(1);

            to.AssertValuesOnly(1);

            subj.OnNext(2);

            to.AssertValuesOnly(1, 2);

            to.Dispose();

            Assert.False(subj.HasObservers);
        }

        [Test]
        public void Dispose_Task()
        {
            var ts = new TestScheduler();
            var count = 0;

            var to = ObservableSource.FromFunc(() => ++count)
                .SubscribeOn(ts)
                .Test();

            to.AssertEmpty();

            Assert.True(ts.HasTasks());

            to.Dispose();

            ts.AdvanceTimeBy(1);

            Assert.AreEqual(0, count);
        }
    }
}
