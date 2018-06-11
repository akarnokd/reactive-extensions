using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.observablesource
{
    [TestFixture]
    public class ObservableSourceIgnoreElementsTest
    {
        [Test]
        public void Regular_Basic()
        {
            ObservableSource.Range(1, 5)
                .IgnoreElements()
                .Test()
                .AssertResult();
        }

        [Test]
        public void Regular_Empty()
        {
            ObservableSource.Empty<int>()
                .IgnoreElements()
                .Test()
                .AssertResult();
        }

        [Test]
        public void Regular_Error()
        {
            ObservableSource.Error<int>(new InvalidOperationException())
                .IgnoreElements()
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Regular_Dispose()
        {
            var subj = new PublishSubject<int>();

            var to = subj.IgnoreElements().Test();

            to.AssertEmpty();

            Assert.True(subj.HasObservers);

            to.Dispose();

            Assert.False(subj.HasObservers);
        }

        [Test]
        public void Fused_Basic()
        {
            ObservableSource.Range(1, 5)
                .IgnoreElements()
                .Test(fusionMode: FusionSupport.Any)
                .AssertFuseable()
                .AssertFusionMode(FusionSupport.Async)
                .AssertResult();
        }

        [Test]
        public void Fused_Rejected()
        {
            ObservableSource.Range(1, 5)
                .IgnoreElements()
                .Test(fusionMode: FusionSupport.Sync)
                .AssertFuseable()
                .AssertFusionMode(FusionSupport.None)
                .AssertResult();
        }

        [Test]
        public void Fused_Empty()
        {
            ObservableSource.Empty<int>()
                .IgnoreElements()
                .Test(fusionMode: FusionSupport.Any)
                .AssertFuseable()
                .AssertFusionMode(FusionSupport.Async)
                .AssertResult();
        }

        [Test]
        public void Fused_Error()
        {
            ObservableSource.Error<int>(new InvalidOperationException())
                .IgnoreElements()
                .Test(fusionMode: FusionSupport.Any)
                .AssertFuseable()
                .AssertFusionMode(FusionSupport.Async)
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Fused_Dispose()
        {
            var subj = new PublishSubject<int>();

            var to = subj.IgnoreElements()
                .Test(fusionMode: FusionSupport.Any)
                .AssertFuseable()
                .AssertFusionMode(FusionSupport.Async);

            to.AssertEmpty();

            Assert.True(subj.HasObservers);

            to.Dispose();

            Assert.False(subj.HasObservers);
        }

        [Test]
        public void Fused_Api()
        {
            var subj = new PublishSubject<int>();

            TestHelper.AssertFuseableApi(subj.IgnoreElements());
        }

    }
}
