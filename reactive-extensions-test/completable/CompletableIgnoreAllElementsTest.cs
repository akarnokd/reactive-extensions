using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Reactive.Linq;

namespace akarnokd.reactive_extensions_test.completable
{
    [TestFixture]
    public class CompletableIgnoreAllElementsTest
    {
        [Test]
        public void Basic()
        {
            Observable.Range(1, 5)
                .IgnoreAllElements()
                .Test()
                .AssertResult();
        }

        [Test]
        public void Error()
        {
            Observable.Range(1, 5).ConcatError(new InvalidOperationException())
                .IgnoreAllElements()
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }


        [Test]
        public void Dispose()
        {
            var us = new UnicastSubject<int>();

            var to = us
                .IgnoreAllElements()
                .Test();

            Assert.True(us.HasObserver());

            to.Dispose();

            Assert.False(us.HasObserver());
        }
    }
}
