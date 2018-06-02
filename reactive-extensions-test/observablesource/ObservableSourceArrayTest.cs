using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.observablesource
{
    [TestFixture]
    public class ObservableSourceArrayTest
    {
        [Test]
        public void From_Basic()
        {
            ObservableSource.FromArray(1, 2, 3, 4, 5)
                .Test()
                .AssertResult(1, 2, 3, 4, 5);
        }

        [Test]
        public void From_Take()
        {
            ObservableSource.FromArray(1, 2, 3, 4, 5)
                .Take(3)
                .Test()
                .AssertResult(1, 2, 3);
        }

        [Test]
        public void From_Take_Zero()
        {
            ObservableSource.FromArray(1, 2, 3, 4, 5)
                .Take(0)
                .Test()
                .AssertResult();
        }

        [Test]
        public void From_Take_Exact()
        {
            ObservableSource.FromArray(1, 2, 3, 4, 5)
                .Take(5)
                .Test()
                .AssertResult(1, 2, 3, 4, 5);
        }

        [Test]
        public void From_Empty()
        {
            ObservableSource.FromArray<int>()
                .Test()
                .AssertResult();
        }

        [Test]
        public void From_Single()
        {
            ObservableSource.FromArray(1)
                .Test()
                .AssertResult(1);
        }

        [Test]
        public void From_Fused()
        {
            ObservableSource.FromArray(1, 2, 3, 4, 5)
                .Test(fusionMode: FusionSupport.Any)
                .AssertFuseable()
                .AssertFusionMode(FusionSupport.Sync)
                .AssertResult(1, 2, 3, 4, 5);
        }

        [Test]
        public void From_FusionRejected()
        {
            ObservableSource.FromArray(1, 2, 3, 4, 5)
                .Test(fusionMode: FusionSupport.Async)
                .AssertFuseable()
                .AssertFusionMode(FusionSupport.None)
                .AssertResult(1, 2, 3, 4, 5);
        }

        [Test]
        public void To_Basic()
        {
            new[] { 1, 2, 3, 4, 5 }.ToObservableSource()
                .Test()
                .AssertResult(1, 2, 3, 4, 5);
        }
    }
}
