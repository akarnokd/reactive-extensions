using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.observablesource
{
    [TestFixture]
    public class ObservableSourceEmptyTest
    {
        [Test]
        public void Basic()
        {
            ObservableSource.Empty<int>()
                .Test()
                .AssertResult();
        }

        [Test]
        public void Scalar()
        {
            var scalar = ObservableSource.Empty<int>() as IStaticValue<int>;

            Assert.AreEqual(default(int), scalar.GetValue(out var success));
            Assert.False(success);
        }
    }
}
