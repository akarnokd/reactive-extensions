using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Reactive.Linq;

namespace akarnokd.reactive_extensions_test.observable
{
    [TestFixture]
    public class RetryWhenTest
    {
        [Test]
        public void Basic_No_Error()
        {
            Observable.Range(1, 5)
                .RetryWhen(v => v)
                .Test()
                .AssertResult(1, 2, 3, 4, 5);
        }

        [Test]
        public void Handler_Completes_Immediately()
        {
            var us = new UnicastSubject<int>();

            us
                .RetryWhen(v => Observable.Empty<int>())
                .Test()
                .AssertResult();

            Assert.False(us.HasObserver());
        }

        [Test]
        public void Retry()
        {
            var count = 0;

            Observable.Defer(() =>
            {
                var o = Observable.Return(1);
                if (++count < 5)
                {
                    o = o.ConcatError(new InvalidOperationException());
                }
                return o;
            })
            .RetryWhen(v => v)
            .Test()
            .AssertResult(1, 1, 1, 1, 1);
        }

        [Test]
        public void Handler_Disposed()
        {
            var us = new UnicastSubject<int>();

            Observable.Range(1, 5)
                .RetryWhen(v => us)
                .Test()
                .AssertResult(1, 2, 3, 4, 5);

            Assert.False(us.HasObserver());
        }

        [Test]
        public void Handler_Errors()
        {
            Observable.Range(1, 5).ConcatError(new InvalidOperationException())
                .RetryWhen(v => v.Take(1).Skip(1).ConcatError(new NotImplementedException()))
                .Test()
                .AssertFailure(typeof(NotImplementedException), 1, 2, 3, 4, 5);
        }

        [Test]
        public void Handler_Completes()
        {
            Observable.Range(1, 5).ConcatError(new InvalidOperationException())
                .RetryWhen(v => v.Take(1).Skip(1))
                .Test()
                .AssertResult(1, 2, 3, 4, 5);
        }

        [Test]
        public void Handler_Crash()
        {
            Observable.Range(1, 5).RetryWhen<int, int>(v => throw new InvalidOperationException())
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }
    }
}
