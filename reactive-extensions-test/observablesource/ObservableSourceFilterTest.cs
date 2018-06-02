using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.observablesource
{
    [TestFixture]
    public class ObservableSourceFilterTest
    {
        [Test]
        public void Regular_Basic()
        {
            ObservableSource.Range(1, 10)
                .Filter(v => v % 2 == 0)
                .Test()
                .AssertResult(2, 4, 6, 8, 10);
        }

        [Test]
        public void Regular_Take()
        {
            ObservableSource.Range(1, 10)
                .Filter(v => v % 2 == 0)
                .Take(3)
                .Test()
                .AssertResult(2, 4, 6);
        }

        [Test]
        public void Regular_Twice()
        {
            ObservableSource.Range(1, 10)
                .Filter(v => v % 2 == 0)
                .Filter(v => v % 3 == 0)
                .Test()
                .AssertResult(6);
        }

        [Test]
        public void Regular_Error()
        {
            ObservableSource.Error<int>(new InvalidOperationException())
                .Filter(v => v % 2 == 0)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Fused_Basic()
        {
            ObservableSource.Range(1, 10)
                .Filter(v => v % 2 == 0)
                .Test(fusionMode: FusionSupport.Any)
                .AssertFuseable()
                .AssertFusionMode(FusionSupport.Sync)
                .AssertResult(2, 4, 6, 8, 10);
        }

        [Test]
        public void Fused_Empty()
        {
            ObservableSource.Empty<int>()
                .Filter(v => v % 2 == 0)
                .Test(fusionMode: FusionSupport.Any)
                .AssertFuseable()
                .AssertFusionMode(FusionSupport.Async)
                .AssertResult();
        }

        [Test]
        public void Fused_Twice()
        {
            ObservableSource.Range(1, 10)
                .Filter(v => v % 2 == 0)
                .Filter(v => v % 3 == 0)
                .Test(fusionMode: FusionSupport.Any)
                .AssertFuseable()
                .AssertFusionMode(FusionSupport.Sync)
                .AssertResult(6);
        }

        [Test]
        public void Fused_Error()
        {
            ObservableSource.Error<int>(new InvalidOperationException())
                .Filter(v => v % 2 == 0)
                .Test(fusionMode: FusionSupport.Any)
                .AssertFuseable()
                .AssertFusionMode(FusionSupport.Async)
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Regular_Mapper_Crash()
        {
            ObservableSource.Range(1, 10)
                .Filter(v => {
                    if (v == 7)
                    {
                        throw new InvalidOperationException();
                    }
                    return v % 2 == 0;
                })
                .Test()
                .AssertFailure(typeof(InvalidOperationException), 2, 4, 6);
        }

        [Test]
        public void Fused_Mapper_Crash()
        {
            ObservableSource.Range(1, 10)
                .Filter(v => {
                    if (v == 7)
                    {
                        throw new InvalidOperationException();
                    }
                    return v % 2 == 0;
                })
                .Test(fusionMode: FusionSupport.Any)
                .AssertFuseable()
                .AssertFusionMode(FusionSupport.Sync)
                .AssertFailure(typeof(InvalidOperationException), 2, 4, 6);
        }
    }
}
