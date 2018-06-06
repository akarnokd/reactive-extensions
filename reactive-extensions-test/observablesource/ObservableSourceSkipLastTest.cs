using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.observablesource
{
    [TestFixture]
    public class ObservableSourceSkipLastTest
    {
        [Test]
        public void Basic()
        {
            ObservableSource.Range(1, 5)
                .SkipLast(2)
                .Test()
                .AssertResult(1, 2, 3);
        }

        [Test]
        public void All()
        {
            ObservableSource.Range(1, 5)
                .SkipLast(10)
                .Test()
                .AssertResult();
        }

        [Test]
        public void None()
        {
            ObservableSource.Range(1, 5)
                .SkipLast(0)
                .Test()
                .AssertResult(1, 2, 3, 4, 5);
        }

        [Test]
        public void Error()
        {
            ObservableSource.Error<int>(new InvalidOperationException())
                .SkipLast(2)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Dispose()
        {
            TestHelper.VerifyDisposeObservableSource<int, int>(o => o.SkipLast(2));
        }
    }
}
