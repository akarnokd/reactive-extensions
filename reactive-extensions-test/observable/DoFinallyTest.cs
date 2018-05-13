using System;
using NUnit.Framework;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.observable
{
    [TestFixture]
    public class DoFinallyTest
    {
        [Test]
        public void Basic_Complete()
        {
            var count = 0;

            var up = new UnicastSubject<int>();

            var ts = up.DoFinally(() => count++).Test();

            up.EmitAll(1, 2, 3, 4, 5);

            ts.AssertResult(1, 2, 3, 4, 5);
            ts.Dispose();

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Basic_Error()
        {
            var count = 0;

            var up = new UnicastSubject<int>();

            var ts = up.DoFinally(() => count++).Test();

            up.EmitError(new InvalidOperationException(), 1, 2, 3, 4, 5);

            ts.AssertFailure(typeof(InvalidOperationException), 1, 2, 3, 4, 5);
            ts.Dispose();

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Basic_Dispose()
        {
            var count = 0;

            var up = new UnicastSubject<int>();

            var ts = up.DoFinally(() => count++).Test();

            Assert.True(up.HasObserver());

            ts.Dispose();

            Assert.False(up.HasObserver());

            Assert.AreEqual(1, count);
        }
    }
}
