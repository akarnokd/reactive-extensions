using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.single
{
    [TestFixture]
    public class SingleFromFuncTest
    {
        [Test]
        public void Basic()
        {
            SingleSource.FromFunc(() => 1)
                .Test()
                .AssertResult(1);
        }

        [Test]
        public void Error()
        {
            SingleSource.FromFunc<int>(() => { throw new InvalidOperationException(); })
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Dispose_Upfront()
        {
            var count = 0;

            SingleSource.FromFunc(() => ++count)
                .Test(true)
                .AssertEmpty();

            Assert.AreEqual(0, count);
        }

        [Test]
        public void Dispose_During()
        {
            var to = new TestObserver<int>();

            SingleSource.FromFunc(() => {
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

            SingleSource.FromFunc<int>(() => {
                to.Dispose();
                throw new InvalidOperationException();
            })
            .SubscribeWith(to)
            .AssertEmpty();

            Assert.True(to.IsDisposed());
        }
    }
}
