using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Reactive.Linq;

namespace akarnokd.reactive_extensions_test.completable
{
    [TestFixture]
    public class CompletableRetryWhenTest
    {
        [Test]
        public void Basic_No_Error()
        {
            var subs = 0;

            CompletableSource.FromAction(() => subs++)
                .RetryWhen(v => v)
                .Test()
                .AssertResult();

            Assert.AreEqual(1, subs);
        }

        [Test]
        public void Handler_Completes_Immediately()
        {
            var us = new CompletableSubject();

            us
                .RetryWhen(v => Observable.Empty<int>())
                .Test()
                .AssertResult();

            Assert.False(us.HasObserver());
        }

        [Test]
        public void Handler_Errors_Immediately()
        {
            var us = new CompletableSubject();

            us
                .RetryWhen(v => Observable.Throw<int>(new InvalidOperationException()))
                .Test()
                .AssertFailure(typeof(InvalidOperationException));

            Assert.False(us.HasObserver());
        }

        [Test]
        public void Retry()
        {
            var count = 0;

            CompletableSource.Defer(() =>
            {
                if (++count < 5)
                {
                    return CompletableSource.Error(new InvalidOperationException());
                }
                return CompletableSource.Empty();
            })
            .RetryWhen(v => v)
            .Test()
            .AssertResult();
        }

        [Test]
        public void Handler_Disposed()
        {
            var us = new UnicastSubject<int>();

            CompletableSource.Empty()
                .RetryWhen(v => us)
                .Test()
                .AssertResult();

            Assert.False(us.HasObserver());
        }

        [Test]
        public void Handler_Errors()
        {
            CompletableSource.Error(new InvalidOperationException())
                .RetryWhen(v => v.Take(1).Skip(1).ConcatError(new NotImplementedException()))
                .Test()
                .AssertFailure(typeof(NotImplementedException));
        }

        [Test]
        public void Handler_Completes()
        {
            CompletableSource.Error(new InvalidOperationException())
                .RetryWhen(v => v.Take(1).Skip(1))
                .Test()
                .AssertResult();
        }
    }
}
