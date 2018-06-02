using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.observablesource
{
    [TestFixture]
    public class ObservableSourceRangeTest
    {
        [Test]
        public void Int_Basic()
        {
            ObservableSource.Range(1, 5)
                .Test()
                .AssertResult(1, 2, 3, 4, 5);
        }

        [Test]
        public void Int_Take()
        {
            ObservableSource.Range(1, 5)
                .Take(3)
                .Test()
                .AssertResult(1, 2, 3);
        }

        [Test]
        public void Int_Take_Zero()
        {
            ObservableSource.Range(1, 5)
                .Take(0)
                .Test()
                .AssertResult();
        }

        [Test]
        public void Int_Take_Exact()
        {
            ObservableSource.Range(1, 5)
                .Take(5)
                .Test()
                .AssertResult(1, 2, 3, 4, 5);
        }

        [Test]
        public void Int_Empty()
        {
            ObservableSource.Range(1, 0)
                .Test()
                .AssertResult();
        }

        [Test]
        public void Int_Single()
        {
            ObservableSource.Range(1, 1)
                .Test()
                .AssertResult(1);
        }

        [Test]
        public void Int_Fused()
        {
            ObservableSource.Range(1, 5)
                .Test(fusionMode: FusionSupport.Any)
                .AssertFuseable()
                .AssertFusionMode(FusionSupport.Sync)
                .AssertResult(1, 2, 3, 4, 5);
        }

        [Test]
        public void Int_RejectedFusion()
        {
            ObservableSource.Range(1, 5)
                .Test(fusionMode: FusionSupport.Async)
                .AssertFuseable()
                .AssertFusionMode(FusionSupport.None)
                .AssertResult(1, 2, 3, 4, 5);
        }

        [Test]
        public void Long_Basic()
        {
            ObservableSource.RangeLong(1, 5)
                .Test()
                .AssertResult(1L, 2L, 3L, 4L, 5L);
        }


        [Test]
        public void Long_Take()
        {
            ObservableSource.RangeLong(1, 5)
                .Take(3)
                .Test()
                .AssertResult(1L, 2L, 3L);
        }

        [Test]
        public void Long_Take_Zero()
        {
            ObservableSource.RangeLong(1, 5)
                .Take(0)
                .Test()
                .AssertResult();
        }

        [Test]
        public void Long_Take_Exact()
        {
            ObservableSource.RangeLong(1, 5)
                .Take(5)
                .Test()
                .AssertResult(1L, 2L, 3L, 4L, 5L);
        }

        [Test]
        public void Long_Empty()
        {
            ObservableSource.RangeLong(1, 0)
                .Test()
                .AssertResult();
        }

        [Test]
        public void Long_Single()
        {
            ObservableSource.RangeLong(1, 1)
                .Test()
                .AssertResult(1L);
        }

        [Test]
        public void Long_Fused()
        {
            ObservableSource.RangeLong(1, 5)
                .Test(fusionMode: FusionSupport.Any)
                .AssertFuseable()
                .AssertFusionMode(FusionSupport.Sync)
                .AssertResult(1L, 2L, 3L, 4L, 5L);
        }

        [Test]
        public void Long_RejectedFusion()
        {
            ObservableSource.RangeLong(1, 5)
                .Test(fusionMode: FusionSupport.Async)
                .AssertFuseable()
                .AssertFusionMode(FusionSupport.None)
                .AssertResult(1L, 2L, 3L, 4L, 5L);
        }
    }
}
