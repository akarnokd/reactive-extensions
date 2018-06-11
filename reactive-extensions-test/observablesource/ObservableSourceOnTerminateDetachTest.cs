using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.observablesource
{
    [TestFixture]
    public class ObservableSourceOnTerminateDetachTest
    {
        [Test]
        public void Basic()
        {
            ObservableSource.Range(1, 5)
                .OnTerminateDetach()
                .Test()
                .AssertResult(1, 2, 3, 4, 5);
        }

        [Test]
        public void Error()
        {
            ObservableSource.Range(1, 5).ConcatError(new InvalidOperationException())
                .OnTerminateDetach()
                .Test()
                .AssertFailure(typeof(InvalidOperationException), 1, 2, 3, 4, 5);
        }

        [Test]
        public void Dispose()
        {
            TestHelper.VerifyDisposeObservableSource<int, int>(o => o.OnTerminateDetach());
        }
    }
}
