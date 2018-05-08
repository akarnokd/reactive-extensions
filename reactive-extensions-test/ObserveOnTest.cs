using System;
using NUnit.Framework;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test
{
    [TestFixture]
    public class ObserveOnTest
    {
        [Test]
        public void Basic_NoDelayError()
        {
            var us = new UnicastSubject<int>();

            var ts = us.ObserveOn(ImmediateScheduler.INSTANCE, false).Test();

            Assert.True(us.HasObserver());

            ts.AssertEmpty();

            us.EmitAll(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);

            Assert.False(us.HasObserver());

            ts.AssertResult(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
        }

        [Test]
        public void Basic_WithDelayError()
        {
            var us = new UnicastSubject<int>();

            var ts = us.ObserveOn(ImmediateScheduler.INSTANCE, true).Test();

            Assert.True(us.HasObserver());

            ts.AssertEmpty();

            us.EmitAll(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);

            Assert.False(us.HasObserver());

            ts.AssertResult(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
        }

        [Test]
        public void Error()
        {
            var us = new UnicastSubject<int>();

            var ts = us.ObserveOn(ImmediateScheduler.INSTANCE, false).Test();

            Assert.True(us.HasObserver());

            ts.AssertEmpty();

            us.EmitError(new InvalidOperationException(), 1, 2, 3, 4, 5, 6, 7, 8, 9, 10);

            ts.AssertFailure(typeof(InvalidOperationException), 1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
        }


        [Test]
        public void Error_Delayed()
        {
            var us = new UnicastSubject<int>();

            var ts = us.ObserveOn(ImmediateScheduler.INSTANCE, true).Test();

            Assert.True(us.HasObserver());

            ts.AssertEmpty();

            us.EmitError(new InvalidOperationException(), 1, 2, 3, 4, 5, 6, 7, 8, 9, 10);

            ts.AssertFailure(typeof(InvalidOperationException), 1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
        }
    }
}
