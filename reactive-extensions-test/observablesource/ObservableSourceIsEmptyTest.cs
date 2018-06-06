using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.observablesource
{
    [TestFixture]
    public class ObservableSourceIsEmptyTest
    {
        [Test]
        public void Basic()
        {
            ObservableSource.Range(1, 5)
                .IsEmpty()
                .Test()
                .AssertResult(false);
        }

        [Test]
        public void Empty()
        {
            ObservableSource.Empty<int>()
                .IsEmpty()
                .Test()
                .AssertResult(true);
        }

        [Test]
        public void Fused()
        {
            ObservableSource.Range(1, 5)
                .IsEmpty()
                .Test(fusionMode: FusionSupport.Any)
                .AssertFuseable()
                .AssertFusionMode(FusionSupport.Async)
                .AssertResult(false);
        }

        [Test]
        public void Fused_Empty()
        {
            ObservableSource.Empty<int>()
                .IsEmpty()
                .Test(fusionMode: FusionSupport.Any)
                .AssertFuseable()
                .AssertFusionMode(FusionSupport.Async)
                .AssertResult(true);
        }

        [Test]
        public void Error()
        {
            ObservableSource.Error<int>(new InvalidOperationException())
                .IsEmpty()
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Dispose()
        {
            TestHelper.VerifyDisposeObservableSource<int, bool>(o => o.IsEmpty());
        }

        [Test]
        public void Dispose_Eager()
        {
            var ps = new PublishSubject<int>();

            var to = ps.IsEmpty().Test();

            Assert.True(ps.HasObservers);

            ps.OnNext(1);

            Assert.False(ps.HasObservers);

            to.AssertResult(false);
        }
    }
}
