using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.completable
{
    [TestFixture]
    public class CompletableToMaybeTest
    {
        [Test]
        public void Completed_Complete()
        {
            CompletableSource.Empty()
                .ToMaybe<int>()
                .Test()
                .AssertResult();
        }

        [Test]
        public void Completed_Success()
        {
            CompletableSource.Empty()
                .ToMaybe(1)
                .Test()
                .AssertResult(1);
        }

        [Test]
        public void Error()
        {
            CompletableSource.Error(new InvalidOperationException())
                .ToMaybe(1)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Dispose_Success()
        {
            var cs = new CompletableSubject();

            var to = cs
                .ToMaybe(1)
                .Test();

            Assert.True(cs.HasObserver());

            to.Dispose();

            Assert.False(cs.HasObserver());
        }

        [Test]
        public void Dispose_Empty()
        {
            var cs = new CompletableSubject();

            var to = cs
                .ToMaybe<int>()
                .Test();

            Assert.True(cs.HasObserver());

            to.Dispose();

            Assert.False(cs.HasObserver());
        }
    }
}
