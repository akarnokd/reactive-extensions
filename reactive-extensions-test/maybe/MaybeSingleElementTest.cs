using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Reactive.Linq;

namespace akarnokd.reactive_extensions_test.maybe
{
    [TestFixture]
    public class MaybeSingleElementTest
    {
        [Test]
        public void Basic_Empty()
        {
            Observable.Empty<int>()
                .SingleElement()
                .Test()
                .AssertResult();
        }

        [Test]
        public void Basic_Just()
        {
            Observable.Return(1)
                .SingleElement()
                .Test()
                .AssertResult(1);
        }

        [Test]
        public void Basic_Range()
        {
            Observable.Range(1, 5)
                .SingleElement()
                .Test()
                .AssertFailure(typeof(IndexOutOfRangeException));
        }

        [Test]
        public void Error()
        {
            Observable.Throw<int>(new InvalidOperationException())
                .SingleElement()
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Dispose()
        {
            TestHelper.VerifyDisposeObservable<int, int>(o => o.SingleElement());
        }
    }
}
