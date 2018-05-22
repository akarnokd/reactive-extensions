using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Reactive.Linq;

namespace akarnokd.reactive_extensions_test.single
{
    [TestFixture]
    public class SingleLastOrErrorTest
    {
        [Test]
        public void Basic()
        {
            Observable.Range(1, 5)
                .LastOrError()
                .Test()
                .AssertResult(5);
        }

        [Test]
        public void Empty()
        {
            Observable.Empty<int>()
                .LastOrError()
                .Test()
                .AssertFailure(typeof(IndexOutOfRangeException));
        }

        [Test]
        public void Error()
        {
            Observable.Throw<int>(new InvalidOperationException())
                .LastOrError()
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Dispose()
        {
            TestHelper.VerifyDisposeObservable<int, int>(o => o.LastOrError());
        }
    }
}
