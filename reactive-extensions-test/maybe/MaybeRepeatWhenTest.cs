using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace akarnokd.reactive_extensions_test.maybe
{
    [TestFixture]
    public class MaybeRepeatWhenTest
    {
        [Test]
        public void Basic()
        {
            var count = 0;

            MaybeSource.FromFunc(() => ++count)
                .RepeatWhen(v => v)
                .SubscribeOn(NewThreadScheduler.Default)
                .Take(5)
                .Test()
                .AwaitDone(TimeSpan.FromSeconds(5))
                .AssertResult(1, 2, 3, 4, 5);

            Assert.True(count >= 5, $"{count}");
        }

        [Test]
        public void Empty()
        {
            var count = 0;

            MaybeSource.FromAction<int>(() =>
            {
                if (++count >= 5)
                {
                    throw new InvalidOperationException();
                }
            })
            .RepeatWhen(v => v)
            .Test()
            .AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(5, count);
        }

        [Test]
        public void Error()
        {
            var count = 0;

            MaybeSource.FromAction<int>(() =>
            {
                count++;
                throw new InvalidOperationException();
            })
            .RepeatWhen(v => v)
            .Test()
            .AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Dispose()
        {
            var ms = new MaybeSubject<int>();
            var subj = new Subject<int>();

            var to = ms.RepeatWhen(v => subj).Test();

            Assert.True(ms.HasObserver());
            Assert.True(subj.HasObservers);

            to.Dispose();

            Assert.False(ms.HasObserver());
            Assert.False(subj.HasObservers);
        }

        [Test]
        public void Handler_Completes_Prematurely()
        {
            var ms = new MaybeSubject<int>();
            var subj = new Subject<int>();

            var to = ms.RepeatWhen(v => subj).Test();

            Assert.True(ms.HasObserver());
            Assert.True(subj.HasObservers);

            subj.OnCompleted();

            Assert.False(ms.HasObserver());
            Assert.False(subj.HasObservers);

            to.AssertResult();
        }

        [Test]
        public void Handler_Fails_Prematurely()
        {
            var ms = new MaybeSubject<int>();
            var subj = new Subject<int>();

            var to = ms.RepeatWhen(v => subj).Test();

            Assert.True(ms.HasObserver());
            Assert.True(subj.HasObservers);

            subj.OnError(new InvalidOperationException());

            Assert.False(ms.HasObserver());
            Assert.False(subj.HasObservers);

            to.AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Limited_Repeat()
        {
            var count = 0;

            MaybeSource.FromFunc(() => ++count)
                .RepeatWhen(v =>
                {
                    var idx = 0;
                    return v.TakeWhile(w => ++idx < 5);
                })
                .Test()
                .AssertResult(1, 2, 3, 4, 5);

            Assert.AreEqual(5, count);
        }
    }
}
