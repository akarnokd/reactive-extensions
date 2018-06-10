using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.observablesource
{
    [TestFixture]
    public class ObservableSourceRefCountTest
    {
        [Test]
        public void Publish_Basic()
        {
            var count = 0;

            var source = ObservableSource.Defer(() =>
            {
                count++;

                return ObservableSource.Range(1, 5);
            })
            .Publish()
            .RefCount();

            Assert.AreEqual(0, count);

            var to0 = source.Test();

            Assert.AreEqual(1, count);

            to0.AssertResult(1, 2, 3, 4, 5);

            Assert.AreEqual(1, count);

            var to = source.Test();

            Assert.AreEqual(2, count);

            to.AssertResult(1, 2, 3, 4, 5);

            Assert.AreEqual(2, count);
        }

        [Test]
        public void Publish_Error()
        {
            var count = 0;

            var source = ObservableSource.Defer(() =>
            {
                count++;

                return ObservableSource.Range(1, 5).ConcatError(new InvalidOperationException());
            })
            .Publish()
            .RefCount();

            Assert.AreEqual(0, count);

            source.Test()
                .AssertFailure(typeof(InvalidOperationException), 1, 2, 3, 4, 5);

            Assert.AreEqual(1, count);

            source.Test()
                .AssertFailure(typeof(InvalidOperationException), 1, 2, 3, 4, 5);

            Assert.AreEqual(2, count);
        }

        [Test]
        public void Publish_Min_Observers()
        {
            var count = 0;

            var source = ObservableSource.Defer(() =>
            {
                count++;

                return ObservableSource.Range(1, 5);
            })
            .Publish()
            .RefCount(2);

            Assert.AreEqual(0, count);

            var to1 = source.Test();

            Assert.AreEqual(0, count);

            var to2 = source.Test();

            Assert.AreEqual(1, count);

            to1.AssertResult(1, 2, 3, 4, 5);
            to2.AssertResult(1, 2, 3, 4, 5);

            var to3 = source.Test();

            Assert.AreEqual(1, count);

            var to4 = source.Test();

            Assert.AreEqual(2, count);

            to3.AssertResult(1, 2, 3, 4, 5);
            to4.AssertResult(1, 2, 3, 4, 5);
        }

        [Test]
        public void Publish_Timeout()
        {
            var ts = new TestScheduler();

            var subj = new PublishSubject<int>();

            var src = subj.Publish().RefCount(TimeSpan.FromSeconds(1), ts);

            Assert.False(subj.HasObservers);

            var to = src.Test();

            Assert.True(subj.HasObservers);

            to.Dispose();

            Assert.True(subj.HasObservers);

            ts.AdvanceTimeBy(1000);

            Assert.False(subj.HasObservers);
        }

        [Test]
        public void Publish_Timeout_Revive()
        {
            var ts = new TestScheduler();

            var subj = new PublishSubject<int>();

            var src = subj.Publish().RefCount(TimeSpan.FromSeconds(1), ts);

            Assert.False(subj.HasObservers);

            var to = src.Test();

            Assert.True(subj.HasObservers);

            to.Dispose();

            Assert.True(subj.HasObservers);

            ts.AdvanceTimeBy(500);

            var to2 = src.Test();

            ts.AdvanceTimeBy(500);

            Assert.True(subj.HasObservers);

            subj.OnCompleted();

            Assert.False(ts.HasTasks());
        }

        [Test]
        public void Replay_Basic()
        {
            var count = 0;

            var source = ObservableSource.Defer(() =>
            {
                count++;

                return ObservableSource.Range(1, 5);
            })
            .Replay()
            .RefCount();

            Assert.AreEqual(0, count);

            var to0 = source.Test();

            Assert.AreEqual(1, count);

            to0.AssertResult(1, 2, 3, 4, 5);

            Assert.AreEqual(1, count);

            var to = source.Test();

            Assert.AreEqual(2, count);

            to.AssertResult(1, 2, 3, 4, 5);

            Assert.AreEqual(2, count);
        }

        [Test]
        public void Replay_Error()
        {
            var count = 0;

            var source = ObservableSource.Defer(() =>
            {
                count++;

                return ObservableSource.Range(1, 5).ConcatError(new InvalidOperationException());
            })
            .Replay()
            .RefCount();

            Assert.AreEqual(0, count);

            source.Test()
                .AssertFailure(typeof(InvalidOperationException), 1, 2, 3, 4, 5);

            Assert.AreEqual(1, count);

            source.Test()
                .AssertFailure(typeof(InvalidOperationException), 1, 2, 3, 4, 5);

            Assert.AreEqual(2, count);
        }

        [Test]
        public void Replay_Min_Observers()
        {
            var count = 0;

            var source = ObservableSource.Defer(() =>
            {
                count++;

                return ObservableSource.Range(1, 5);
            })
            .Replay()
            .RefCount(2);

            Assert.AreEqual(0, count);

            var to1 = source.Test();

            Assert.AreEqual(0, count);

            var to2 = source.Test();

            Assert.AreEqual(1, count);

            to1.AssertResult(1, 2, 3, 4, 5);
            to2.AssertResult(1, 2, 3, 4, 5);

            var to3 = source.Test();

            Assert.AreEqual(1, count);

            var to4 = source.Test();

            Assert.AreEqual(2, count);

            to3.AssertResult(1, 2, 3, 4, 5);
            to4.AssertResult(1, 2, 3, 4, 5);
        }

        [Test]
        public void Replay_Timeout()
        {
            var ts = new TestScheduler();

            var subj = new PublishSubject<int>();

            var src = subj.Replay().RefCount(TimeSpan.FromSeconds(1), ts);

            Assert.False(subj.HasObservers);

            var to = src.Test();

            Assert.True(subj.HasObservers);

            to.Dispose();

            Assert.True(subj.HasObservers);

            ts.AdvanceTimeBy(1000);

            Assert.False(subj.HasObservers);
        }

        [Test]
        public void Replay_Timeout_Revive()
        {
            var ts = new TestScheduler();

            var subj = new PublishSubject<int>();

            var src = subj.Replay().RefCount(TimeSpan.FromSeconds(1), ts);

            Assert.False(subj.HasObservers);

            var to = src.Test();

            Assert.True(subj.HasObservers);

            to.Dispose();

            Assert.True(subj.HasObservers);

            ts.AdvanceTimeBy(500);

            var to2 = src.Test();

            ts.AdvanceTimeBy(500);

            Assert.True(subj.HasObservers);

            subj.OnCompleted();

            Assert.False(ts.HasTasks());
        }
    }
}
