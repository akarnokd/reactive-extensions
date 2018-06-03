using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Linq;

namespace akarnokd.reactive_extensions_test.observablesource
{
    [TestFixture]
    public class ObservableSourceEnumerableTest
    {
        [Test]
        public void Regular_Basic()
        {
            Enumerable.Range(1, 5)
                .ToObservableSource()
                .Test()
                .AssertResult(1, 2, 3, 4, 5);
        }

        [Test]
        public void Regular_Empty()
        {
            Enumerable.Empty<int>()
                .ToObservableSource()
                .Test()
                .AssertResult();
        }

        [Test]
        public void Regular_GetEnumerator_Throws()
        {
            new FailingEnumerable<int>(true, false, false)
                .ToObservableSource()
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Regular_MoveNext_Throws()
        {
            new FailingEnumerable<int>(false, true, false)
                .ToObservableSource()
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Fused_Basic()
        {
            ObservableSource.FromEnumerable(Enumerable.Range(1, 5))
                .Test(fusionMode: FusionSupport.Any)
                .AssertFuseable()
                .AssertFusionMode(FusionSupport.Sync)
                .AssertResult(1, 2, 3, 4, 5);
        }

        [Test]
        public void Fused_Empty()
        {
            Enumerable.Empty<int>()
                .ToObservableSource()
                .Test(fusionMode: FusionSupport.Any)
                .AssertFuseable()
                .AssertFusionMode(FusionSupport.Sync)
                .AssertResult();
        }
    }
}
