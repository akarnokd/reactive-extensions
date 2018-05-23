using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Reactive.Linq;

namespace akarnokd.reactive_extensions_test.completable
{
    [TestFixture]
    public class CompletableAndThenTest
    {
        #region + Observable +

        [Test]
        public void Observable_Basic()
        {
            CompletableSource.Empty()
                .AndThen(Observable.Range(1, 5))
                .Test()
                .AssertResult(1, 2, 3, 4, 5);
        }

        [Test]
        public void Observable_Dispose_Main()
        {
            var cs = new CompletableSubject();

            var to = cs
                .AndThen(Observable.Range(1, 5))
                .Test();

            Assert.True(cs.HasObserver());

            to.Dispose();

            Assert.False(cs.HasObserver());
        }

        [Test]
        public void Observable_Dispose_Next()
        {
            var us = new UnicastSubject<int>();

            var to = CompletableSource.Empty()
                .AndThen(us)
                .Test();

            Assert.True(us.HasObserver());

            to.Dispose();

            Assert.False(us.HasObserver());
        }

        [Test]
        public void Observable_Error()
        {
            var us = new UnicastSubject<int>();
            
            CompletableSource.Error(new InvalidOperationException())
                .AndThen(us)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));

            Assert.False(us.HasObserver());
        }

        [Test]
        public void Observable_Then_Error()
        {
            CompletableSource.Empty()
                .AndThen(Observable.Throw<int>(new InvalidOperationException()))
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        #endregion Observable

        #region + Completable +

        [Test]
        public void Completable_Basic()
        {
            var count = 0;

            CompletableSource.Empty()
                .AndThen(CompletableSource.FromAction(() => count++))
                .Test()
                .AssertResult();

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Completable_Dispose_Main()
        {
            var cs = new CompletableSubject();
            var count = 0;

            var to = cs
                .AndThen(CompletableSource.FromAction(() => count++))
                .Test();

            Assert.True(cs.HasObserver());

            to.Dispose();

            Assert.False(cs.HasObserver());

            Assert.AreEqual(0, count);
        }

        [Test]
        public void Completable_Dispose_Next()
        {
            var us = new CompletableSubject();

            var to = CompletableSource.Empty()
                .AndThen(us)
                .Test();

            Assert.True(us.HasObserver());

            to.Dispose();

            Assert.False(us.HasObserver());
        }

        [Test]
        public void Completable_Error()
        {
            var us = new CompletableSubject();

            CompletableSource.Error(new InvalidOperationException())
                .AndThen(us)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));

            Assert.False(us.HasObserver());
        }

        [Test]
        public void Completable_Then_Error()
        {
            CompletableSource.Empty()
                .AndThen(CompletableSource.Error(new InvalidOperationException()))
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        #endregion Completable

        #region Maybe

        [Test]
        public void Maybe_Basic()
        {
            CompletableSource.Empty()
                .AndThen(MaybeSource.Just(1))
                .Test()
                .AssertResult(1);
        }

        [Test]
        public void Maybe_Main_Error()
        {
            CompletableSource.Error(new InvalidOperationException())
                .AndThen(MaybeSource.Just(1))
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Maybe_Next_Error()
        {
            CompletableSource.Empty()
                .AndThen(MaybeSource.Error<int>(new InvalidOperationException()))
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Maybe_Next_Empty()
        {
            CompletableSource.Empty()
                .AndThen(MaybeSource.Empty<int>())
                .Test()
                .AssertResult();
        }

        [Test]
        public void Maybe_Dispose_Main()
        {
            var cs = new CompletableSubject();

            var to = cs
                .AndThen(MaybeSource.Just(1))
                .Test();

            Assert.True(cs.HasObserver());

            to.Dispose();

            Assert.False(cs.HasObserver());
        }

        [Test]
        public void Maybe_Dispose_Other()
        {
            var ms = new MaybeSubject<int>();

            var to = CompletableSource.Empty()
                .AndThen(ms)
                .Test();

            Assert.True(ms.HasObserver());

            to.Dispose();

            Assert.False(ms.HasObserver());
        }

        #endregion Maybe

        #region + Single +

        [Test]
        public void Single_Basic()
        {
            CompletableSource.Empty()
                .AndThen(SingleSource.Just(1))
                .Test()
                .AssertResult(1);
        }

        [Test]
        public void Single_Main_Error()
        {
            CompletableSource.Error(new InvalidOperationException())
                .AndThen(SingleSource.Just(1))
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Single_Next_Error()
        {
            CompletableSource.Empty()
                .AndThen(SingleSource.Error<int>(new InvalidOperationException()))
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Single_Dispose_Main()
        {
            var cs = new CompletableSubject();

            var to = cs
                .AndThen(SingleSource.Just(1))
                .Test();

            Assert.True(cs.HasObserver());

            to.Dispose();

            Assert.False(cs.HasObserver());
        }

        [Test]
        public void Single_Dispose_Other()
        {
            var ms = new SingleSubject<int>();

            var to = CompletableSource.Empty()
                .AndThen(ms)
                .Test();

            Assert.True(ms.HasObserver());

            to.Dispose();

            Assert.False(ms.HasObserver());
        }

        #endregion Single
    }
}
