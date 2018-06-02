using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.observablesource
{
    [TestFixture]
    public class ObservableSourceErrorTest
    {
        [Test]
        public void Basic()
        {
            ObservableSource.Error<int>(new InvalidOperationException())
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Scalar()
        {
            var scalar = ObservableSource.Error<int>(new InvalidOperationException()) as IDynamicValue<int>;

            try
            {
                scalar.GetValue(out var success);
                Assert.Fail();
            }
            catch (InvalidOperationException expected)
            {
                // okay
            }
        }
    }
}
