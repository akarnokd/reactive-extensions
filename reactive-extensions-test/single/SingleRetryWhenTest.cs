using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Reactive.Subjects;
using System.Reactive.Linq;

namespace akarnokd.reactive_extensions_test.single
{
    [TestFixture]
    public class SingleRetryWhenTest
    {
        [Test]
        public void Basic()
        {
            var count = 0;

            SingleSource.FromFunc(() => ++count)
                .RetryWhen(v => v)
                .Test()
                .AssertResult(1);

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Error()
        {
            var count = 0;

            SingleSource.FromFunc<int>(() =>
            {
                if (++count < 5)
                {
                    throw new InvalidOperationException();
                }
                return count;
            })
            .RetryWhen(v => v)
            .Test()
            .AssertResult(5);
        }

        [Test]
        public void Dispose()
        {
            var ms = new SingleSubject<int>();
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
            var ms = new SingleSubject<int>();
            var subj = new Subject<int>();

            var to = ms.RetryWhen(v => subj).Test();

            Assert.True(ms.HasObserver());
            Assert.True(subj.HasObservers);

            subj.OnCompleted();

            Assert.False(ms.HasObserver());
            Assert.False(subj.HasObservers);

            to.AssertFailure(typeof(IndexOutOfRangeException));
        }

        [Test]
        public void Handler_Fails_Prematurely()
        {
            var ms = new SingleSubject<int>();
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

            SingleSource.FromFunc<int>(() => {
                ++count;
                throw new InvalidOperationException();
            })
                .RetryWhen(v =>
                {
                    var idx = 0;
                    return v.TakeWhile(w => ++idx < 5);
                })
                .Test()
                .AssertFailure(typeof(IndexOutOfRangeException));

            Assert.AreEqual(5, count);
        }

        [Test]
        public void Error_To_Error()
        {
            var count = 0;

            SingleSource.FromFunc<int>(() =>
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
