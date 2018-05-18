using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.completable
{
    [TestFixture]
    public class CompletableOnErrorCompleteTest
    {
        [Test]
        public void Basic()
        {
            CompletableSource.Empty()
                .OnErrorComplete()
                .Test()
                .AssertResult();
        }

        [Test]
        public void Error()
        {
            CompletableSource.Error(new InvalidOperationException())
                .OnErrorComplete()
                .Test()
                .AssertResult();
        }

        [Test]
        public void Dispose()
        {
            var cs = new CompletableSubject();

            var to = cs
                .OnErrorComplete()
                .Test();

            Assert.True(cs.HasObserver());

            to.Dispose();

            Assert.False(cs.HasObserver());
        }
    }
}
