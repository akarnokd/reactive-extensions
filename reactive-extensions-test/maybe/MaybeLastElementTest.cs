using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Reactive.Linq;

namespace akarnokd.reactive_extensions_test.maybe
{
    [TestFixture]
    public class MaybeLastElementTest
    {
        [Test]
        public void Basic()
        {
            Observable.Range(1, 5)
                .LastElement()
                .Test()
                .AssertResult(5);
        }

        [Test]
        public void Empty()
        {
            Observable.Empty<int>()
                .LastElement()
                .Test()
                .AssertResult();
        }

        [Test]
        public void Error()
        {
            Observable.Throw<int>(new InvalidOperationException())
                .LastElement()
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Dispose()
        {
            TestHelper.VerifyDisposeObservable<int, int>(o => o.LastElement());
        }
    }
}
