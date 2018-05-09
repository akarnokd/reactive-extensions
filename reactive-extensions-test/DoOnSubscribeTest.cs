using System;
using NUnit.Framework;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test
{
    [TestFixture]
    public class DoOnSubscribeTest
    {
        [Test]
        public void Basic()
        {
            var count = 0;

            var up = new UnicastSubject<int>();

            var src = up.DoOnSubscribe(() => count++);

            for (int i = 1; i < 6; i++) {
                src.Test();
                Assert.AreEqual(i, count);
            }
        }

        [Test]
        public void Basic_Handler_Crash()
        {
            var up = new UnicastSubject<int>();

            var src = up.DoOnSubscribe(() => { throw new InvalidOperationException();  });

            src.Test().AssertFailure(typeof(InvalidOperationException));
        }
    }
}
