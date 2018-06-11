using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Reactive.Linq;

namespace akarnokd.reactive_extensions_test.observablesource
{
    [TestFixture]
    public class ObservableSourceRetryWhenTest
    {
        [Test]
        public void Basic_No_Error()
        {
            ObservableSource.Range(1, 5)
                .RetryWhen(v => v)
                .Test()
                .AssertResult(1, 2, 3, 4, 5);
        }

        [Test]
        public void Handler_Completes_Immediately()
        {
            var us = new MonocastSubject<int>();

            us
                .RetryWhen(v => ObservableSource.Empty<int>())
                .Test()
                .AssertResult();

            Assert.False(us.HasObserver());
        }

        [Test]
        public void Retry()
        {
            var count = 0;

            ObservableSource.Defer(() =>
            {
                var o = ObservableSource.Just(1);
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
            var us = new MonocastSubject<int>();

            ObservableSource.Range(1, 5)
                .RetryWhen(v => us)
                .Test()
                .AssertResult(1, 2, 3, 4, 5);

            Assert.False(us.HasObserver());
        }

        [Test]
        public void Handler_Errors()
        {
            ObservableSource.Range(1, 5).ConcatError(new InvalidOperationException())
                .RetryWhen(v => v.Take(1).Skip(1).ConcatError(new NotImplementedException()))
                .Test()
                .AssertFailure(typeof(NotImplementedException), 1, 2, 3, 4, 5);
        }

        [Test]
        public void Handler_Completes()
        {
            ObservableSource.Range(1, 5).ConcatError(new InvalidOperationException())
                .RetryWhen(v => v.Take(1).Skip(1))
                .Test()
                .AssertResult(1, 2, 3, 4, 5);
        }

        [Test]
        public void Dispose()
        {
            TestHelper.VerifyDisposeObservableSource<int, int>(o => o.RetryWhen(v => v));
        }

        [Test]
        public void Handler_Crash()
        {
            ObservableSource.Range(1, 5)
                .RetryWhen<int, int>(v => throw new InvalidOperationException())
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }
    }
}
