using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.observablesource
{
    [TestFixture]
    public class ObservableSourceDefer
    {
        [Test]
        public void Basic()
        {
            var count = 0;

            var source = ObservableSource.Defer(() => ObservableSource.Just(++count));

            for (int i = 0; i < 10; i++)
            {
                source.Test().AssertResult(i + 1);
            }

            Assert.AreEqual(10, count);
        }

        [Test]
        public void Supplier_Crash()
        {
            ObservableSource.Defer<int>(() => throw new InvalidOperationException())
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }
    }
}
