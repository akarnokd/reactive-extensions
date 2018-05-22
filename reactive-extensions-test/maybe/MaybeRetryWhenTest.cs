using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Reactive.Subjects;
using System.Reactive.Linq;

namespace akarnokd.reactive_extensions_test.maybe
{
    [TestFixture]
    public class MaybeRetryWhenTest
    {
        [Test]
        public void Basic()
        {
            var count = 0;

            MaybeSource.FromFunc(() => ++count)
                .RetryWhen(v => v)
                .Test()
                .AssertResult(1);

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Empty()
        {
            var count = 0;

            MaybeSource.FromAction<int>(() => ++count)
                .RetryWhen(v => v)
                .Test()
                .AssertResult();

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Error()
        {
            var count = 0;

            MaybeSource.FromAction<int>(() =>
            {
                if (++count < 5)
                {
                    throw new InvalidOperationException();
                }
            })
            .RetryWhen(v => v)
            .Test()
            .AssertResult();
        }

        [Test]
        public void Dispose()
        {
            var ms = new MaybeSubject<int>();
            var subj = new Subject<int>();

            var to = ms.RetryWhen(v => subj).Test();

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

            var to = ms.RetryWhen(v => subj).Test();

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

            var to = ms.RetryWhen(v => subj).Test();

            Assert.True(ms.HasObserver());
            Assert.True(subj.HasObservers);

            subj.OnError(new InvalidOperationException());

            Assert.False(ms.HasObserver());
            Assert.False(subj.HasObservers);

            to.AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Limited_Retry()
        {
            var count = 0;

            MaybeSource.FromAction<int>(() => {
                ++count;
                throw new InvalidOperationException();
            })
                .RetryWhen(v =>
                {
                    var idx = 0;
                    return v.TakeWhile(w => ++idx < 5);
                })
                .Test()
                .AssertResult();

            Assert.AreEqual(5, count);
        }

        [Test]
        public void Error_To_Error()
        {
            var count = 0;

            MaybeSource.FromAction<int>(() =>
            {
                ++count;
                throw new InvalidOperationException();
            })
            .RetryWhen(v => v.Select<Exception, int>(w => throw w))
            .Test()
            .AssertFailure(typeof(InvalidOperationException));
        }
    }
}
