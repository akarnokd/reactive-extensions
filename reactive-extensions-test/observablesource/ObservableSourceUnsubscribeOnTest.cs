using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Reactive.Concurrency;
using System.Threading;

namespace akarnokd.reactive_extensions_test.observablesource
{
    [TestFixture]
    public class ObservableSourceUnsubscribeOnTest
    {
        [Test]
        public void Basic()
        {
            var name = "";
            
            ObservableSource.Range(1, 5)
                .DoOnDispose(() => name = Thread.CurrentThread.Name)
                .UnsubscribeOn(ThreadPoolScheduler.Instance)
                .Test()
                .AssertResult(1, 2, 3, 4, 5);

            Thread.Sleep(100);

            Assert.AreEqual("", name);
        }

        [Test]
        public void Error()
        {
            var name = "";

            ObservableSource.Error<int>(new InvalidOperationException())
                .DoOnDispose(() => name = Thread.CurrentThread.Name)
                .UnsubscribeOn(ThreadPoolScheduler.Instance)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));

            Thread.Sleep(100);

            Assert.AreEqual("", name);
        }

        [Test]
        public void Dispose()
        {
            var subj = new PublishSubject<int>();

            var name = "";

            var to = subj
                .DoOnDispose(() => name = Thread.CurrentThread.Name)
                .UnsubscribeOn(CurrentThreadScheduler.Instance)
                .Test();

            Assert.True(subj.HasObservers);

            to.AssertEmpty();

            Assert.AreEqual("", name);

            to.Dispose();

            Assert.False(subj.HasObservers);

            Assert.AreEqual(Thread.CurrentThread.Name, name);
        }

        [Test]
        public void Dispose_Suppress_OnNext_OnCompleted()
        {
            var ts = new TestScheduler();
            var subj = new PublishSubject<int>();

            var name = "";

            var to = subj
                .DoOnDispose(() => name = Thread.CurrentThread.Name)
                .UnsubscribeOn(ts)
                .Test();

            Assert.True(subj.HasObservers);

            to.AssertEmpty();

            Assert.AreEqual("", name);

            to.Dispose();

            Assert.True(subj.HasObservers);

            subj.OnNext(1);

            to.AssertEmpty();

            subj.OnCompleted();

            to.AssertEmpty();

            Assert.AreEqual("", name);

            ts.AdvanceTimeBy(1);

            Assert.False(subj.HasObservers);
            Assert.AreEqual(Thread.CurrentThread.Name, name);

            to.AssertEmpty();
        }

        [Test]
        public void Dispose_Suppress_OnNext_OnError()
        {
            var ts = new TestScheduler();
            var subj = new PublishSubject<int>();

            var name = "";

            var to = subj
                .DoOnDispose(() => name = Thread.CurrentThread.Name)
                .UnsubscribeOn(ts)
                .Test();

            Assert.True(subj.HasObservers);

            to.AssertEmpty();

            Assert.AreEqual("", name);

            to.Dispose();

            Assert.True(subj.HasObservers);

            subj.OnNext(1);

            to.AssertEmpty();

            subj.OnError(new InvalidOperationException());

            to.AssertEmpty();

            Assert.AreEqual("", name);

            ts.AdvanceTimeBy(1);

            Assert.False(subj.HasObservers);
            Assert.AreEqual(Thread.CurrentThread.Name, name);

            to.AssertEmpty();
        }
    }
}
