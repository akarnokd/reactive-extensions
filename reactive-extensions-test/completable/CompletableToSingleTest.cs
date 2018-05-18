using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.completable
{
    [TestFixture]
    public class CompletableToSingleTest
    {
        [Test]
        public void Completed()
        {
            CompletableSource.Empty()
                .ToSingle(1)
                .Test()
                .AssertResult(1);
        }

        [Test]
        public void Error()
        {
            CompletableSource.Error(new InvalidOperationException())
                .ToSingle(1)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Dispose()
        {
            var cs = new CompletableSubject();

            var to = cs
                .ToSingle(1)
                .Test();

            Assert.True(cs.HasObserver());

            to.Dispose();

            Assert.False(cs.HasObserver());
        }
    }
}
