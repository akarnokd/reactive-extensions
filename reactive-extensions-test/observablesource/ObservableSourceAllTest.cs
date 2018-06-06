using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.observablesource
{
    [TestFixture]
    public class ObservableSourceAllTest
    {
        [Test]
        public void Basic()
        {
            ObservableSource.Range(1, 5)
                .All(v => v < 6)
                .Test()
                .AssertResult(true);
        }

        [Test]
        public void Empty()
        {
            ObservableSource.Empty<int>()
                .All(v => v < 6)
                .Test()
                .AssertResult(true);
        }

        [Test]
        public void Not_Found()
        {
            ObservableSource.Range(1, 5)
                .All(v => v < 5)
                .Test()
                .AssertResult(false);
        }

        [Test]
        public void Fused()
        {
            ObservableSource.Range(1, 5)
                .All(v => v < 6)
                .Test(fusionMode: FusionSupport.Any)
                .AssertFuseable()
                .AssertFusionMode(FusionSupport.Async)
                .AssertResult(true);
        }

        [Test]
        public void Error()
        {
            ObservableSource.Error<int>(new InvalidOperationException())
                .All(v => v == 3)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Predicate_Crash()
        {
            ObservableSource.Range(1, 5)
                .All(v => {
                    if (v == 3)
                    {
                        throw new InvalidOperationException();
                    }
                    return true;
                })
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Dispose()
        {
            TestHelper.VerifyDisposeObservableSource<int, bool>(o => o.All(v => false));
        }
    }
}
