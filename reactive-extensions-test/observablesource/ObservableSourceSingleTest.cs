using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.observablesource
{
    [TestFixture]
    public class ObservableSourceSingleTest
    {
        [Test]
        public void Plain_Basic()
        {
            ObservableSource.Just(1)
                .Single()
                .Test()
                .AssertResult(1);
        }

        [Test]
        public void Plain_Fused()
        {
            ObservableSource.Just(1)
                .Single()
                .Test(fusionMode: FusionSupport.Any)
                .AssertFuseable()
                .AssertFusionMode(FusionSupport.Async)
                .AssertResult(1);
        }

        [Test]
        public void Plain_More()
        {
            ObservableSource.Range(1, 5)
                .Single()
                .Test()
                .AssertFailure(typeof(IndexOutOfRangeException));
        }

        [Test]
        public void Plain_Empty()
        {
            ObservableSource.Empty<int>()
                .Single()
                .Test()
                .AssertFailure(typeof(IndexOutOfRangeException));
        }

        [Test]
        public void Plain_Error()
        {
            ObservableSource.Error<int>(new InvalidOperationException())
                .Single()
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Plain_Dispose()
        {
            TestHelper.VerifyDisposeObservableSource<int, int>(o => o.Single());
        }

        [Test]
        public void Plain_Second_Disposes()
        {
            var ps = new PublishSubject<int>();

            var to = ps.Single().Test();

            to.AssertEmpty();

            Assert.True(ps.HasObservers);

            ps.OnNext(1);

            to.AssertEmpty();

            ps.OnNext(2);

            to.AssertFailure(typeof(IndexOutOfRangeException));

            Assert.False(ps.HasObservers);
        }

        [Test]
        public void Default_Basic()
        {
            ObservableSource.Just(1)
                .Single(100)
                .Test()
                .AssertResult(1);
        }

        [Test]
        public void Default_Fused()
        {
            ObservableSource.Just(1)
                .Single(100)
                .Test(fusionMode: FusionSupport.Any)
                .AssertFuseable()
                .AssertFusionMode(FusionSupport.Async)
                .AssertResult(1);
        }

        [Test]
        public void Default_More()
        {
            ObservableSource.Range(1, 5)
                .Single(100)
                .Test()
                .AssertFailure(typeof(IndexOutOfRangeException));
        }

        [Test]
        public void Default_Empty()
        {
            ObservableSource.Empty<int>()
                .Single(100)
                .Test()
                .AssertResult(100);
        }

        [Test]
        public void Default_Error()
        {
            ObservableSource.Error<int>(new InvalidOperationException())
                .Single(100)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Default_Dispose()
        {
            TestHelper.VerifyDisposeObservableSource<int, int>(o => o.Single(100));
        }

        [Test]
        public void Default_Second_Disposes()
        {
            var ps = new PublishSubject<int>();

            var to = ps.Single(100).Test();

            to.AssertEmpty();

            Assert.True(ps.HasObservers);

            ps.OnNext(1);

            to.AssertEmpty();

            ps.OnNext(2);

            to.AssertFailure(typeof(IndexOutOfRangeException));

            Assert.False(ps.HasObservers);
        }
    }
}
