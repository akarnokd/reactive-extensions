using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.completable
{
    [TestFixture]
    public class CompletableLambdaObserverTest
    {
        [Test]
        public void Basic_No_Arguments()
        {
            CompletableSource.Empty()
                .Subscribe();
        }

        [Test]
        public void Basic_Complete()
        {
            var complete = 0;

            CompletableSource.Empty()
                .Subscribe(() => complete++);

            Assert.AreEqual(1, complete);
        }

        [Test]
        public void Basic()
        {
            var complete = 0;
            var error = default(Exception);

            CompletableSource.Empty()
                .Subscribe(() => complete++, e => error = e);

            Assert.AreEqual(1, complete);
            Assert.IsNull(error);
        }

        [Test]
        public void Error()
        {
            var complete = 0;
            var error = default(Exception);

            CompletableSource.Error(new InvalidOperationException())
                .Subscribe(() => complete++, e => error = e);

            Assert.AreEqual(0, complete);
            Assert.True(typeof(InvalidOperationException).IsAssignableFrom(error));
        }

        [Test]
        public void Dispose()
        {
            var cs = new CompletableSubject();

            var complete = 0;
            var error = default(Exception);

            var d = cs.Subscribe(() => complete++, e => error = e);

            Assert.True(cs.HasObserver());

            d.Dispose();

            Assert.False(cs.HasObserver());

            Assert.AreEqual(0, complete);
            Assert.IsNull(error);
        }

        [Test]
        public void OnError_Crash()
        {
            CompletableSource.Error(new IndexOutOfRangeException())
                .Subscribe(() => { }, v => throw new InvalidOperationException());
        }

        [Test]
        public void OnCompleted_Crash()
        {
            var error = default(Exception);

            CompletableSource.Empty()
                .Subscribe(() => throw new InvalidOperationException(), e => error = e);

            Assert.Null(error);
        }
    }
}
