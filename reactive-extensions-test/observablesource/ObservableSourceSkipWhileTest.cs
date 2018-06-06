using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.observablesource
{
    [TestFixture]
    public class ObservableSourceSkipWhileTest
    {
        [Test]
        public void Basic()
        {
            ObservableSource.Range(1, 5)
                .SkipWhile(v => v < 3)
                .Test()
                .AssertResult(3, 4, 5);
        }

        [Test]
        public void Skip_All()
        {
            ObservableSource.Range(1, 5)
                .SkipWhile(v => v < 6)
                .Test()
                .AssertResult();
        }

        [Test]
        public void Skip_None()
        {
            ObservableSource.Range(1, 5)
                .SkipWhile(v => v < 1)
                .Test()
                .AssertResult(1, 2, 3, 4, 5);
        }

        [Test]
        public void Error()
        {
            ObservableSource.Error<int>(new InvalidOperationException())
                .SkipWhile(v => v < 1)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Predicate_Crash()
        {
            ObservableSource.Range(1, 5)
                .SkipWhile(v => throw new InvalidOperationException())
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Dispose()
        {
            TestHelper.VerifyDisposeObservableSource<int, int>(o => o.SkipWhile(v => v < 3));
        }
    }
}
