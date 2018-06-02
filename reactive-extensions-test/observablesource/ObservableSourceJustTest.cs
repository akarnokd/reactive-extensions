using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.observablesource
{
    [TestFixture]
    public class ObservableSourceJustTest
    {
        [Test]
        public void Basic()
        {
            ObservableSource.Just(1)
                .Test()
                .AssertResult(1);
        }

        [Test]
        public void Scalar()
        {
            var scalar = ObservableSource.Just(1) as IStaticValue<int>;

            Assert.AreEqual(1, scalar.GetValue(out var success));
            Assert.True(success);
        }

        [Test]
        public void Fusion_Sync()
        {
            ObservableSource.Just(1)
                .Test(fusionMode: FusionSupport.Sync)
                .AssertFuseable()
                .AssertFusionMode(FusionSupport.Sync)
                .AssertResult(1);
        }
    }
}
