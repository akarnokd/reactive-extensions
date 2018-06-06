using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.observablesource
{
    [TestFixture]
    public class ObservableSourceLastTest
    {
        [Test]
        public void Plain_Basic()
        {
            ObservableSource.Range(1, 5)
                .Last()
                .Test()
                .AssertResult(5);
        }

        [Test]
        public void Plain_Fused()
        {
            ObservableSource.Range(1, 5)
                .Last()
                .Test(fusionMode: FusionSupport.Any)
                .AssertFuseable()
                .AssertFusionMode(FusionSupport.Async)
                .AssertResult(5);
        }

        [Test]
        public void Plain_Empty()
        {
            ObservableSource.Empty<int>()
                .Last()
                .Test()
                .AssertFailure(typeof(IndexOutOfRangeException));
        }

        [Test]
        public void Plain_Error()
        {
            ObservableSource.Error<int>(new InvalidOperationException())
                .Last()
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Plain_Dispose()
        {
            TestHelper.VerifyDisposeObservableSource<int, int>(o => o.Last());
        }

        [Test]
        public void Default_Basic()
        {
            ObservableSource.Range(1, 5)
                .Last(100)
                .Test()
                .AssertResult(5);
        }

        [Test]
        public void Default_Fused()
        {
            ObservableSource.Range(1, 5)
                .Last(100)
                .Test(fusionMode: FusionSupport.Any)
                .AssertFuseable()
                .AssertFusionMode(FusionSupport.Async)
                .AssertResult(5);
        }

        [Test]
        public void Default_Empty()
        {
            ObservableSource.Empty<int>()
                .Last(100)
                .Test()
                .AssertResult(100);
        }

        [Test]
        public void Default_Error()
        {
            ObservableSource.Error<int>(new InvalidOperationException())
                .Last(100)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Default_Dispose()
        {
            TestHelper.VerifyDisposeObservableSource<int, int>(o => o.Last(100));
        }
    }
}
