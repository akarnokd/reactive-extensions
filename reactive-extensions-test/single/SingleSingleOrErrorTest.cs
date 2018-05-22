using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Reactive.Linq;

namespace akarnokd.reactive_extensions_test.single
{
    [TestFixture]
    public class SingleSingleOrErrorTest
    {
        [Test]
        public void Basic_Empty()
        {
            Observable.Empty<int>()
                .SingleOrError()
                .Test()
                .AssertFailure(typeof(IndexOutOfRangeException));
        }

        [Test]
        public void Basic_Just()
        {
            Observable.Return(1)
                .SingleOrError()
                .Test()
                .AssertResult(1);
        }

        [Test]
        public void Basic_Range()
        {
            Observable.Range(1, 5)
                .SingleOrError()
                .Test()
                .AssertFailure(typeof(IndexOutOfRangeException));
        }

        [Test]
        public void Error()
        {
            Observable.Throw<int>(new InvalidOperationException())
                .SingleOrError()
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Dispose()
        {
            TestHelper.VerifyDisposeObservable<int, int>(o => o.SingleOrError());
        }
    }
}
