using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.completable
{
    [TestFixture]
    public class CompletableHideTest
    {
        [Test]
        public void Basic()
        {
            CompletableSource.Empty()
                .Hide()
                .Test()
                .AssertResult();
        }

        [Test]
        public void Error()
        {
            CompletableSource.Error(new InvalidOperationException())
                .Hide()
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Dispose()
        {
            var cs = new CompletableSubject();

            var to = cs.Hide().Test();

            Assert.True(cs.HasObserver());

            to.Dispose();

            Assert.False(cs.HasObserver());
        }
    }
}
