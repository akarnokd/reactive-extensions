using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.maybe
{
    [TestFixture]
    public class MaybeHideTest
    {
        [Test]
        public void Success()
        {
            MaybeSource.Just(1)
                .Hide()
                .Test()
                .AssertResult(1);
        }

        [Test]
        public void Complete()
        {
            MaybeSource.Empty<int>()
                .Hide()
                .Test()
                .AssertResult();
        }

        [Test]
        public void Error()
        {
            MaybeSource.Error<int>(new InvalidOperationException())
                .Hide()
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Dispose()
        {
            var cs = new MaybeSubject<int>();

            var to = cs.Hide().Test();

            Assert.True(cs.HasObserver());

            to.Dispose();

            Assert.False(cs.HasObserver());
        }
    }
}
