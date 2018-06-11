using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.single
{
    [TestFixture]
    public class SingleLambdaObserverTest
    {
        [Test]
        public void Success()
        {
            var count = 0;

            SingleSource.Just(1)
                .Subscribe(v => { count = v; }, e => { count = 2; });

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Error()
        {
            var count = 0;

            SingleSource.Error<int>(new InvalidOperationException())
                .Subscribe(v => { count = v; }, e => { count = 2; });

            Assert.AreEqual(2, count);
        }

        [Test]
        public void Dispose()
        {
            var count = 0;

            var ss = new SingleSubject<int>();

            var d = ss
                .Subscribe(v => { count = v; }, e => { count = 2; });

            Assert.True(ss.HasObserver());

            d.Dispose();

            Assert.False(ss.HasObserver());
            Assert.AreEqual(0, count);
        }

        [Test]
        public void OnSuccess_Crash()
        {
            var error = default(Exception);

            SingleSource.Just(1)
                .Subscribe(v => throw new InvalidOperationException(), e => error = e);

            Assert.Null(error);
        }

        [Test]
        public void OnError_Crash()
        {
            SingleSource.Error<int>(new IndexOutOfRangeException())
                .Subscribe(v => { }, v => throw new InvalidOperationException());
        }
    }
}
