using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Reactive.Linq;

namespace akarnokd.reactive_extensions_test.completable
{
    [TestFixture]
    public class CompletableAndThenTest
    {
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
    }
}
