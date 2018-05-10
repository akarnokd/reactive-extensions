using System;
using System.Reactive.Linq;
using NUnit.Framework;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test
{
    [TestFixture]
    public class DoAfterTerminateTest
    {
        [Test]
        public void Basic()
        {
            var count = 0;
            var v = -1;

            Observable.Range(1, 5)
                .DoAfterTerminate(() => count = v)
                .Subscribe(u => v = u, e => { }, () => { v = 6; });
            
            Assert.AreEqual(6, v);
            Assert.AreEqual(6, count);
        }

        [Test]
        public void Error()
        {
            var count = 0;
            var v = -1;

            Observable.Range(1, 5)
                .Concat(Observable.Throw<int>(new InvalidOperationException()))
                .DoAfterTerminate(() => count = v)
                .Subscribe(u => v = u, e => { v = 6; });
            
            Assert.AreEqual(6, v);
            Assert.AreEqual(6, count);
        }

    }
}
