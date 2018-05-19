using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.completable
{
    [TestFixture]
    public class CompletableCacheTest
    {
        [Test]
        public void Basic()
        {
            var count = 0;

            var source = CompletableSource.FromAction(() => count++)
                .Cache();

            Assert.AreEqual(0, count);

            source.Test().AssertResult();

            Assert.AreEqual(1, count);

            source.Test().AssertResult();

            source.Test(true).AssertEmpty();
        }

        [Test]
        public void Error()
        {
            var count = 0;

            var source = CompletableSource.FromAction(() => {
                count++;
                throw new InvalidOperationException();
            })
                .Cache();

            Assert.AreEqual(0, count);

            source.Test().AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(1, count);

            source.Test().AssertFailure(typeof(InvalidOperationException));

            source.Test(true).AssertEmpty();
        }

        [Test]
        public void Dispose()
        {
            var d = new IDisposable[1];

            var cs = new CompletableSubject();

            var source = cs.Cache(c => d[0] = c);

            Assert.IsNull(d[0]);
            Assert.IsFalse(cs.HasObserver());

            var to1 = source.Test();

            Assert.IsNotNull(d[0]);
            Assert.IsTrue(cs.HasObserver());

            source.Test(true).AssertEmpty();

            d[0].Dispose();

            Assert.IsFalse(cs.HasObserver());

            to1.AssertFailure(typeof(OperationCanceledException));

            source.Test().AssertFailure(typeof(OperationCanceledException));
        }
    }
}
