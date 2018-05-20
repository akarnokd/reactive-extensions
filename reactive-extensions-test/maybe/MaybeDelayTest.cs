using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Reactive.Concurrency;

namespace akarnokd.reactive_extensions_test.maybe
{
    [TestFixture]
    public class MaybeDelayTest
    {
        [Test]
        public void Success()
        {
            MaybeSource.Just(1)
                .Delay(TimeSpan.FromMilliseconds(100), NewThreadScheduler.Default)
                .Test()
                .AwaitDone(TimeSpan.FromSeconds(5))
                .AssertResult(1);
        }

        [Test]
        public void Basic()
        {
            MaybeSource.Empty<int>()
                .Delay(TimeSpan.FromMilliseconds(100), NewThreadScheduler.Default)
                .Test()
                .AwaitDone(TimeSpan.FromSeconds(5))
                .AssertResult();
        }

        [Test]
        public void Error()
        {
            MaybeSource.Error<int>(new InvalidOperationException())
                .Delay(TimeSpan.FromMilliseconds(100), NewThreadScheduler.Default)
                .Test()
                .AwaitDone(TimeSpan.FromSeconds(5))
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Dispose()
        {
            TestHelper.VerifyDisposeMaybe<int, int>(m =>
                m   
                .Delay(TimeSpan.FromMilliseconds(100), NewThreadScheduler.Default)

            );
        }

        [Test]
        public void Basic_Step()
        {
            var ts = new TestScheduler();

            var to = MaybeSource.Empty<int>()
                .Delay(TimeSpan.FromSeconds(1), ts).Test();

            to.AssertEmpty();

            ts.AdvanceTimeBy(500);

            to.AssertEmpty();

            ts.AdvanceTimeBy(500);

            to.AssertResult();
        }
        [Test]
        public void Success_Step()
        {
            var ts = new TestScheduler();

            var to = MaybeSource.Just(1)
                .Delay(TimeSpan.FromSeconds(1), ts).Test();

            to.AssertEmpty();

            ts.AdvanceTimeBy(500);

            to.AssertEmpty();

            ts.AdvanceTimeBy(500);

            to.AssertResult(1);
        }

        [Test]
        public void Error_Step()
        {
            var ts = new TestScheduler();

            var to = MaybeSource.Error<int>(new InvalidOperationException())
                .Delay(TimeSpan.FromSeconds(1), ts).Test();

            to.AssertEmpty();

            ts.AdvanceTimeBy(500);

            to.AssertEmpty();

            ts.AdvanceTimeBy(500);

            to.AssertFailure(typeof(InvalidOperationException));
        }
    }
}
