using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.observablesource
{
    [TestFixture]
    public class ObservableSourceTakeLastTest
    {
        [Test]
        public void Basic()
        {
            ObservableSource.Range(1, 10)
                .TakeLast(5)
                .Test()
                .AssertResult(6, 7, 8, 9, 10);
        }

        [Test]
        public void All()
        {
            ObservableSource.Range(1, 5)
                .TakeLast(10)
                .Test()
                .AssertResult(1, 2, 3, 4, 5);
        }

        [Test]
        public void Exact()
        {
            ObservableSource.Range(1, 5)
                .TakeLast(5)
                .Test()
                .AssertResult(1, 2, 3, 4, 5);
        }

        [Test]
        public void One()
        {
            ObservableSource.Range(1, 5)
                .TakeLast(1)
                .Test()
                .AssertResult(5);
        }

        [Test]
        public void None()
        {
            ObservableSource.Range(1, 5)
                .TakeLast(0)
                .Test()
                .AssertResult();
        }

        [Test]
        public void Error()
        {
            ObservableSource.Error<int>(new InvalidOperationException())
                .TakeLast(3)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Dispose()
        {
            TestHelper.VerifyDisposeObservableSource<int, int>(o => o.TakeLast(3));
        }

        [Test]
        public void Fused()
        {
            ObservableSource.Range(1, 10)
                .TakeLast(5)
                .Test(fusionMode: FusionSupport.Any)
                .AssertFuseable()
                .AssertFusionMode(FusionSupport.Async)
                .AssertResult(6, 7, 8, 9, 10);
        }
    }
}
