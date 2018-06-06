using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.observablesource
{
    [TestFixture]
    public class ObservableSourceReduceTest
    {
        [Test]
        public void Initial_Basic()
        {
            ObservableSource.Range(1, 5)
                .Reduce(() => 0, (a, b) => a + b)
                .Test()
                .AssertResult(15);
        }

        [Test]
        public void Initial_Empty()
        {
            ObservableSource.Empty<int>()
                .Reduce(() => 0, (a, b) => a + b)
                .Test()
                .AssertResult(0);
        }

        [Test]
        public void Initial_Error()
        {
            ObservableSource.Error<int>(new InvalidOperationException())
                .Reduce(() => 0, (a, b) => a + b)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Initial_InitialSupplier_Crash()
        {
            ObservableSource.Just(1)
                .Reduce<int, int>(() => throw new InvalidOperationException(), (a, b) => a + b)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Initial_Reducer_Crash()
        {
            ObservableSource.Just(1)
                .Reduce<int, int>(() => 0, (a, b) => throw new InvalidOperationException())
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Initial_Dispose()
        {
            TestHelper.VerifyDisposeObservableSource<int, int>(o => o.Reduce(() => 0, (a, b) => a + b));
        }

        [Test]
        public void Initial_Fused()
        {
            ObservableSource.Range(1, 5)
                .Reduce(() => 0, (a, b) => a + b)
                .Test(fusionMode: FusionSupport.Any)
                .AssertFuseable()
                .AssertFusionMode(FusionSupport.Async)
                .AssertResult(15);
        }

        [Test]
        public void Plain_Basic()
        {
            ObservableSource.Range(1, 5)
                .Reduce((a, b) => a + b)
                .Test()
                .AssertResult(15);
        }

        [Test]
        public void Plain_Empty()
        {
            ObservableSource.Empty<int>()
                .Reduce((a, b) => a + b)
                .Test()
                .AssertResult();
        }

        [Test]
        public void Plain_Single()
        {
            var count = 0;

            ObservableSource.Just(1)
                .Reduce((a, b) => { count++; return a + b; })
                .Test()
                .AssertResult(1);

            Assert.AreEqual(0, count);
        }

        [Test]
        public void Plain_Error()
        {
            ObservableSource.Error<int>(new InvalidOperationException())
                .Reduce((a, b) => a + b)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Plain_Reducer_Crash()
        {
            ObservableSource.Range(1, 5)
                .Reduce((a, b) => throw new InvalidOperationException())
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Plain_Dispose()
        {
            TestHelper.VerifyDisposeObservableSource<int, int>(o => o.Reduce((a, b) => a + b));
        }

        [Test]
        public void Plain_Fused()
        {
            ObservableSource.Range(1, 5)
                .Reduce((a, b) => a + b)
                .Test(fusionMode: FusionSupport.Any)
                .AssertFuseable()
                .AssertFusionMode(FusionSupport.Async)
                .AssertResult(15);
        }
    }
}
