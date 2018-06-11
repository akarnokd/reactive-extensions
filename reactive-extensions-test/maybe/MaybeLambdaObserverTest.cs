using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.maybe
{
    [TestFixture]
    public class MaybeLambdaObserverTest
    {
        [Test]
        public void Success()
        {
            var count = 0;

            MaybeSource.Just(1)
                .Subscribe(v => { count = v; }, e => { count = 2; }, () => { count = 3; });

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Error()
        {
            var count = 0;

            MaybeSource.Error<int>(new InvalidOperationException())
                .Subscribe(v => { count = v; }, e => { count = 2; }, () => { count = 3; });

            Assert.AreEqual(2, count);
        }

        [Test]
        public void Completed()
        {
            var count = 0;

            MaybeSource.Empty<int>()
                .Subscribe(v => { count = v; }, e => { count = 2; }, () => { count = 3; });

            Assert.AreEqual(3, count);
        }

        [Test]
        public void Dispose()
        {
            var count = 0;

            var ss = new MaybeSubject<int>();

            var d = ss
                .Subscribe(v => { count = v; }, e => { count = 2; }, () => { count = 3; });

            Assert.True(ss.HasObserver());

            d.Dispose();

            Assert.False(ss.HasObserver());
            Assert.AreEqual(0, count);
        }

        [Test]
        public void OnSuccess_Crash()
        {
            var error = default(Exception);

            MaybeSource.Just(1)
                .Subscribe(v => throw new InvalidOperationException(), e => error = e);

            Assert.Null(error);
        }

        [Test]
        public void OnError_Crash()
        {
            MaybeSource.Error<int>(new IndexOutOfRangeException())
                .Subscribe(v => { }, v => throw new InvalidOperationException());
        }

        [Test]
        public void OnCompleted_Crash()
        {
            var error = default(Exception);

            MaybeSource.Empty<int>()
                .Subscribe(v => { }, e => error = e, () => throw new InvalidOperationException());

            Assert.Null(error);
        }

    }
}
