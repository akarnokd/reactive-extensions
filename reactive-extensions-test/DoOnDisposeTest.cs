using System;
using NUnit.Framework;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test
{
    [TestFixture]
    public class DoOnDisposeTest
    {
        [Test]
        public void Basic()
        {
            var count = 0;

            var up = new UnicastSubject<int>();

            var d = up.DoOnDispose(() => count++).Subscribe();

            Assert.True(up.HasObserver());
            Assert.AreEqual(0, count);

            d.Dispose();

            Assert.AreEqual(1, count);

            d.Dispose();

            Assert.AreEqual(1, count);

            Assert.False(up.HasObserver());
        }
    }
}
