using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.maybe
{
    [TestFixture]
    public class MaybeFromFuncTest
    {
        [Test]
        public void Basic()
        {
            MaybeSource.FromFunc(() => 1)
                .Test()
                .AssertResult(1);
        }

        [Test]
        public void Error()
        {
            MaybeSource.FromFunc<int>(() => { throw new InvalidOperationException(); })
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Dispose_Upfront()
        {
            var count = 0;

            MaybeSource.FromFunc(() => ++count)
                .Test(true)
                .AssertEmpty();

            Assert.AreEqual(0, count);
        }

        [Test]
        public void Dispose_During()
        {
            var to = new TestObserver<int>();

            MaybeSource.FromFunc(() => {
                to.Dispose();
                return 1;
            })
            .SubscribeWith(to)
            .AssertEmpty();

            Assert.True(to.IsDisposed());
        }

        [Test]
        public void Dispose_During_Error()
        {
            var to = new TestObserver<int>();

            MaybeSource.FromFunc<int>(() => {
                to.Dispose();
                throw new InvalidOperationException();
            })
            .SubscribeWith(to)
            .AssertEmpty();

            Assert.True(to.IsDisposed());
        }
    }
}
