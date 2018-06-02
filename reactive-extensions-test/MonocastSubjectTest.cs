using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test
{
    [TestFixture]
    public class MonocastSubjectTest
    {
        [Test]
        public void Online_Basic()
        {
            var ms = new MonocastSubject<int>();

            Assert.False(ms.HasObservers);
            Assert.False(ms.HasException());
            Assert.False(ms.HasCompleted());
            Assert.Null(ms.GetException());

            var to = ms.Test();

            ms.Test().AssertFailure(typeof(InvalidOperationException));

            Assert.True(ms.HasObservers);
            Assert.False(ms.HasException());
            Assert.False(ms.HasCompleted());
            Assert.Null(ms.GetException());

            to.AssertEmpty();

            ms.OnNext(1);

            to.AssertValuesOnly(1);

            ms.OnNext(2);

            to.AssertValuesOnly(1, 2);

            ms.OnNext(3);

            to.AssertValuesOnly(1, 2, 3);

            ms.OnCompleted();

            to.AssertResult(1, 2, 3);

            Assert.False(ms.HasObservers);
            Assert.False(ms.HasException());
            Assert.True(ms.HasCompleted());
            Assert.Null(ms.GetException());

            ms.Test().AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Online_Error()
        {
            var ms = new MonocastSubject<int>();

            Assert.False(ms.HasObservers);
            Assert.False(ms.HasException());
            Assert.False(ms.HasCompleted());
            Assert.Null(ms.GetException());

            var to = ms.Test();

            ms.Test().AssertFailure(typeof(InvalidOperationException));

            Assert.True(ms.HasObservers);
            Assert.False(ms.HasException());
            Assert.False(ms.HasCompleted());
            Assert.Null(ms.GetException());

            to.AssertEmpty();

            ms.OnNext(1);

            to.AssertValuesOnly(1);

            ms.OnNext(2);

            to.AssertValuesOnly(1, 2);

            ms.OnNext(3);

            to.AssertValuesOnly(1, 2, 3);

            var ex = new IndexOutOfRangeException();

            ms.OnError(ex);

            to.AssertFailure(typeof(IndexOutOfRangeException), 1, 2, 3);

            Assert.False(ms.HasObservers);
            Assert.True(ms.HasException());
            Assert.False(ms.HasCompleted());
            Assert.AreEqual(ex,ms.GetException());

            ms.Test().AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Online_Basic_Fused()
        {
            var ms = new MonocastSubject<int>();

            Assert.False(ms.HasObservers);
            Assert.False(ms.HasException());
            Assert.False(ms.HasCompleted());
            Assert.Null(ms.GetException());

            var to = ms.Test(fusionMode: FusionSupport.Any);

            to.AssertFuseable()
                .AssertFusionMode(FusionSupport.Async);

            ms.Test().AssertFailure(typeof(InvalidOperationException));

            Assert.True(ms.HasObservers);
            Assert.False(ms.HasException());
            Assert.False(ms.HasCompleted());
            Assert.Null(ms.GetException());

            to.AssertEmpty();

            ms.OnNext(1);

            to.AssertValuesOnly(1);

            ms.OnNext(2);

            to.AssertValuesOnly(1, 2);

            ms.OnNext(3);

            to.AssertValuesOnly(1, 2, 3);

            ms.OnCompleted();

            to.AssertResult(1, 2, 3);

            Assert.False(ms.HasObservers);
            Assert.False(ms.HasException());
            Assert.True(ms.HasCompleted());
            Assert.Null(ms.GetException());

            ms.Test().AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Online_Error_Fused()
        {
            var ms = new MonocastSubject<int>();

            Assert.False(ms.HasObservers);
            Assert.False(ms.HasException());
            Assert.False(ms.HasCompleted());
            Assert.Null(ms.GetException());

            var to = ms.Test(fusionMode: FusionSupport.Any);

            to.AssertFuseable()
                .AssertFusionMode(FusionSupport.Async);

            ms.Test().AssertFailure(typeof(InvalidOperationException));

            Assert.True(ms.HasObservers);
            Assert.False(ms.HasException());
            Assert.False(ms.HasCompleted());
            Assert.Null(ms.GetException());

            to.AssertEmpty();

            ms.OnNext(1);

            to.AssertValuesOnly(1);

            ms.OnNext(2);

            to.AssertValuesOnly(1, 2);

            ms.OnNext(3);

            to.AssertValuesOnly(1, 2, 3);

            var ex = new IndexOutOfRangeException();

            ms.OnError(ex);

            to.AssertFailure(typeof(IndexOutOfRangeException), 1, 2, 3);

            Assert.False(ms.HasObservers);
            Assert.True(ms.HasException());
            Assert.False(ms.HasCompleted());
            Assert.AreEqual(ex, ms.GetException());

            ms.Test().AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void OnTerminate_Completed()
        {
            var count = 0;

            var ms = new MonocastSubject<int>(onTerminate: () => count++);

            ms.OnCompleted();
            ms.OnCompleted();

            Assert.AreEqual(1, count);
        }

        [Test]
        public void OnTerminate_Error()
        {
            var count = 0;

            var ms = new MonocastSubject<int>(onTerminate: () => count++);

            ms.OnError(new InvalidOperationException());
            ms.OnError(new IndexOutOfRangeException());

            Assert.AreEqual(1, count);
        }

        [Test]
        public void OnTerminate_Dispose()
        {
            var count = 0;

            var ms = new MonocastSubject<int>(onTerminate: () => count++);

            ms.Test().Dispose();

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Offline_Basic()
        {
            var ms = new MonocastSubject<int>();
            ms.OnNext(1);
            ms.OnNext(2);
            ms.OnNext(3);
            ms.OnCompleted();

            Assert.False(ms.HasObservers);
            Assert.False(ms.HasException());
            Assert.True(ms.HasCompleted());
            Assert.Null(ms.GetException());

            var to = ms.Test();

            Assert.False(ms.HasObservers);

            to.AssertResult(1, 2, 3);

            ms.Test().AssertFailure(typeof(InvalidOperationException));

        }

        [Test]
        public void Offline_Error()
        {
            var ms = new MonocastSubject<int>();
            var ex = new IndexOutOfRangeException();

            ms.OnNext(1);
            ms.OnNext(2);
            ms.OnNext(3);
            ms.OnError(ex);

            Assert.False(ms.HasObservers);
            Assert.True(ms.HasException());
            Assert.False(ms.HasCompleted());
            Assert.AreEqual(ex, ms.GetException());

            var to = ms.Test();
            to.AssertFailure(typeof(IndexOutOfRangeException), 1, 2, 3);

            Assert.False(ms.HasObservers);

            ms.Test().AssertFailure(typeof(InvalidOperationException));
        }


        [Test]
        public void Offline_Basic_Fused()
        {
            var ms = new MonocastSubject<int>();
            ms.OnNext(1);
            ms.OnNext(2);
            ms.OnNext(3);
            ms.OnCompleted();

            Assert.False(ms.HasObservers);
            Assert.False(ms.HasException());
            Assert.True(ms.HasCompleted());
            Assert.Null(ms.GetException());

            var to = ms.Test(fusionMode: FusionSupport.Any);

            to.AssertFuseable()
                .AssertFusionMode(FusionSupport.Async);

            Assert.False(ms.HasObservers);

            to.AssertResult(1, 2, 3);

            ms.Test().AssertFailure(typeof(InvalidOperationException));

        }

        [Test]
        public void Offline_Error_Fused()
        {
            var ms = new MonocastSubject<int>();
            var ex = new IndexOutOfRangeException();

            ms.OnNext(1);
            ms.OnNext(2);
            ms.OnNext(3);
            ms.OnError(ex);

            Assert.False(ms.HasObservers);
            Assert.True(ms.HasException());
            Assert.False(ms.HasCompleted());
            Assert.AreEqual(ex, ms.GetException());

            var to = ms.Test(fusionMode: FusionSupport.Any);

            Assert.False(ms.HasObservers);

            to.AssertFuseable()
                .AssertFusionMode(FusionSupport.Async);

            to.AssertFailure(typeof(IndexOutOfRangeException), 1, 2, 3);


            ms.Test().AssertFailure(typeof(InvalidOperationException));
        }

    }
}
