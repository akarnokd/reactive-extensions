using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.observablesource
{
    [TestFixture]
    public class ObservableSourceElementAtTest
    {
        [Test]
        public void Plain_Basic()
        {
            ObservableSource.Range(1, 5)
                .ElementAt(2)
                .Test()
                .AssertResult(3);
        }

        [Test]
        public void Plain_First()
        {
            ObservableSource.Range(1, 5)
                .First()
                .Test()
                .AssertResult(1);
        }

        [Test]
        public void Plain_Out_Of_Range()
        {
            ObservableSource.Range(1, 5)
                .ElementAt(5)
                .Test()
                .AssertFailure(typeof(IndexOutOfRangeException));
        }

        [Test]
        public void Plain_Error()
        {
            ObservableSource.Error<int>(new InvalidOperationException())
                .ElementAt(0)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Plain_Error_2()
        {
            ObservableSource.Error<int>(new InvalidOperationException())
                .ElementAt(2)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Plain_Disposed()
        {
            TestHelper.VerifyDisposeObservableSource<int, int>(o => o.ElementAt(2));
        }

        [Test]
        public void Plain_Fused()
        {
            ObservableSource.Range(1, 5)
                .ElementAt(2)
                .Test(fusionMode: FusionSupport.Any)
                .AssertFuseable()
                .AssertFusionMode(FusionSupport.Async)
                .AssertResult(3);
        }

        [Test]
        public void Plain_Dispose_At_Index()
        {
            var ps = new PublishSubject<int>();

            var to = ps
                .ElementAt(2)
                .Test();

            ps.OnNext(1);

            Assert.True(ps.HasObservers);

            ps.OnNext(2);

            Assert.True(ps.HasObservers);

            ps.OnNext(3);

            Assert.False(ps.HasObservers);

            to.AssertResult(3);
        }

        [Test]
        public void Default_Basic()
        {
            ObservableSource.Range(1, 5)
                .ElementAt(2, 100)
                .Test()
                .AssertResult(3);
        }

        [Test]
        public void Default_First()
        {
            ObservableSource.Range(1, 5)
                .First(100)
                .Test()
                .AssertResult(1);
        }

        [Test]
        public void Default_Out_Of_Range()
        {
            ObservableSource.Range(1, 5)
                .ElementAt(5, 100)
                .Test()
                .AssertResult(100);
        }

        [Test]
        public void Default_Error()
        {
            ObservableSource.Error<int>(new InvalidOperationException())
                .ElementAt(0, 100)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Default_Error_2()
        {
            ObservableSource.Error<int>(new InvalidOperationException())
                .ElementAt(2, 100)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Default_Disposed()
        {
            TestHelper.VerifyDisposeObservableSource<int, int>(o => o.ElementAt(2, 100));
        }

        [Test]
        public void Default_Fused()
        {
            ObservableSource.Range(1, 5)
                .ElementAt(2, 100)
                .Test(fusionMode: FusionSupport.Any)
                .AssertFuseable()
                .AssertFusionMode(FusionSupport.Async)
                .AssertResult(3);
        }

        [Test]
        public void Default_Dispose_At_Index()
        {
            var ps = new PublishSubject<int>();

            var to = ps
                .ElementAt(2, 100)
                .Test();

            ps.OnNext(1);

            Assert.True(ps.HasObservers);

            ps.OnNext(2);

            Assert.True(ps.HasObservers);

            ps.OnNext(3);

            Assert.False(ps.HasObservers);

            to.AssertResult(3);
        }
    }
}
