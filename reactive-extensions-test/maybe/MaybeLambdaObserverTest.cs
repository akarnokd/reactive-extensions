using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.maybe
{
    [TestFixture]
    public class MaybeLambdaObserverTest
    {
        [Test]
        public void Success()
        {
            var count = 0;

            MaybeSource.Just(1)
                .Subscribe(v => { count = v; }, e => { count = 2; }, () => { count = 3; });

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Error()
        {
            var count = 0;

            MaybeSource.Error<int>(new InvalidOperationException())
                .Subscribe(v => { count = v; }, e => { count = 2; }, () => { count = 3; });

            Assert.AreEqual(2, count);
        }

        [Test]
        public void Completed()
        {
            var count = 0;

            MaybeSource.Empty<int>()
                .Subscribe(v => { count = v; }, e => { count = 2; }, () => { count = 3; });

            Assert.AreEqual(3, count);
        }
    }
}
