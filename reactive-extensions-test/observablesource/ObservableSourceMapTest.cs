using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.observablesource
{
    [TestFixture]
    public class ObservableSourceMapTest
    {
        [Test]
        public void Regular_Basic()
        {
            ObservableSource.Range(1, 5)
                .Map(v => v.ToString())
                .Test()
                .AssertResult("1", "2", "3", "4", "5");
        }

        [Test]
        public void Regular_Empty()
        {
            ObservableSource.Empty<int>()
                .Map(v => v.ToString())
                .Test()
                .AssertResult();
        }

        [Test]
        public void Regular_Error()
        {
            ObservableSource.Error<int>(new InvalidOperationException())
                .Map(v => v.ToString())
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Regular_Take()
        {
            ObservableSource.Range(1, 5)
                .Map(v => v.ToString())
                .Take(3)
                .Test()
                .AssertResult("1", "2", "3");
        }

        [Test]
        public void Regular_Mapper_Crash()
        {
            ObservableSource.Range(1, 5)
                .Map(v => {
                    if (v == 4)
                    {
                        throw new InvalidOperationException();
                    }
                    return v.ToString();
                })
                .Test()
                .AssertFailure(typeof(InvalidOperationException), "1", "2", "3");
        }

        [Test]
        public void Fused_Basic()
        {
            ObservableSource.Range(1, 5)
                .Map(v => v.ToString())
                .Test(fusionMode: FusionSupport.Any)
                .AssertFuseable()
                .AssertFusionMode(FusionSupport.Sync)
                .AssertResult("1", "2", "3", "4", "5");
        }

        [Test]
        public void Fused_Empty()
        {
            ObservableSource.Empty<int>()
                .Map(v => v.ToString())
                .Test(fusionMode: FusionSupport.Any)
                .AssertFuseable()
                .AssertFusionMode(FusionSupport.Async)
                .AssertResult();
        }

        [Test]
        public void Fused_Error()
        {
            ObservableSource.Error<int>(new InvalidOperationException())
                .Map(v => v.ToString())
                .Test(fusionMode: FusionSupport.Any)
                .AssertFuseable()
                .AssertFusionMode(FusionSupport.Async)
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Fused_Take()
        {
            ObservableSource.Range(1, 5)
                .Map(v => v.ToString())
                .Take(3)
                .Test(fusionMode: FusionSupport.Any)
                .AssertNotFuseable()
                .AssertResult("1", "2", "3");
        }

        [Test]
        public void Fused_Mapper_Crash()
        {
            ObservableSource.Range(1, 5)
                .Map(v => {
                    if (v == 4)
                    {
                        throw new InvalidOperationException();
                    }
                    return v.ToString();
                })
                .Test(fusionMode: FusionSupport.Any)
                .AssertFuseable()
                .AssertFusionMode(FusionSupport.Sync)
                .AssertFailure(typeof(InvalidOperationException), "1", "2", "3");
        }

        [Test]
        public void Fused_Boundary()
        {
            ObservableSource.Range(1, 5)
                .Map(v => v.ToString())
                .Test(fusionMode: FusionSupport.AnyBoundary)
                .AssertFuseable()
                .AssertFusionMode(FusionSupport.None)
                .AssertResult("1", "2", "3", "4", "5");
        }
    }
}
