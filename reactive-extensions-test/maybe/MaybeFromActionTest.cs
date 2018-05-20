using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.maybe
{
    [TestFixture]
    public class MaybeFromActionTest
    {
        [Test]
        public void Basic()
        {
            var count = 0;

            var c = MaybeSource.FromAction<int>(() => count++);

            for (int i = 0; i < 100; i++)
            {
                Assert.AreEqual(i, count);

                c.Test()
                    .AssertSubscribed()
                    .AssertResult();

                Assert.AreEqual(i + 1, count);
            }
        }

        [Test]
        public void Crash()
        {
            var count = 0;

            var c = MaybeSource.FromAction<int>(() =>
            {
                count++;
                throw new InvalidOperationException();
            });

            for (int i = 0; i < 100; i++)
            {
                Assert.AreEqual(i, count);

                c.Test()
                    .AssertSubscribed()
                    .AssertFailure(typeof(InvalidOperationException));

                Assert.AreEqual(i + 1, count);
            }
        }

        [Test]
        public void Disposed_Upfront()
        {
            var count = 0;

            var c = MaybeSource.FromAction<int>(() => count++);

            c.Test(true)
                .AssertSubscribed()
                .AssertEmpty();

            Assert.AreEqual(0, count);
        }

        [Test]
        public void Disposed_During()
        {
            var count = 0;

            var to = new TestObserver<object>();

            var c = MaybeSource.FromAction<object>(() => {
                count++;
                to.Dispose();
            });

            c.SubscribeWith(to)
                .AssertSubscribed()
                .AssertEmpty();

            Assert.AreEqual(1, count);
        }


        [Test]
        public void Disposed_During_Error()
        {
            var count = 0;

            var to = new TestObserver<object>();

            var c = MaybeSource.FromAction<object>(() => {
                count++;
                to.Dispose();
                throw new InvalidOperationException();
            });

            c.SubscribeWith(to)
                .AssertSubscribed()
                .AssertEmpty();

            Assert.AreEqual(1, count);
        }
    }
}
