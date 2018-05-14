using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.completable
{
    [TestFixture]
    public class CompletableToObservableTest
    {
        [Test]
        public void Basic()
        {
            // do not make this var to ensure the target type is correct
            IObservable<int> o = CompletableSource.Empty().ToObservable<int>();

            o.Test().AssertResult();
        }

        [Test]
        public void Error()
        {
            // do not make this var to ensure the target type is correct
            IObservable<int> o = CompletableSource.Error(new InvalidOperationException()).ToObservable<int>();

            o.Test().AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Disposed()
        {
            var up = new CompletableSubject();

            IObservable<int> o = up.ToObservable<int>();

            var to = o.Test();

            Assert.True(up.HasObserver());

            to.Dispose();

            Assert.False(up.HasObserver());

        }
    }
}
