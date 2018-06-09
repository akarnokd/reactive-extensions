using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.observablesource
{
    [TestFixture]
    public class ObservableSourceConcatArrayTest
    {
        [Test]
        public void Basic()
        {
            ObservableSource.Concat(
                ObservableSource.Range(1, 5),
                ObservableSource.Range(6, 5),
                ObservableSource.Range(11, 5)
            )
            .Test()
            .AssertResult(1, 2, 3, 4, 5,
                6, 7, 8, 9, 10, 11, 12, 13, 14, 15);
        }

        [Test]
        public void Empty()
        {
            ObservableSource.Concat<int>()
            .Test()
            .AssertResult();
        }

        [Test]
        public void Empty_Emptys()
        {
            ObservableSource.Concat<int>(
                ObservableSource.Empty<int>(),
                ObservableSource.Empty<int>(),
                ObservableSource.Empty<int>()
            )
            .Test()
            .AssertResult();
        }

        [Test]
        public void Error_Eager()
        {
            ObservableSource.Concat(
                ObservableSource.Range(1, 5),
                ObservableSource.Error<int>(new InvalidOperationException()),
                ObservableSource.Range(11, 5)
            )
            .Test()
            .AssertFailure(typeof(InvalidOperationException), 1, 2, 3, 4, 5);
        }

        [Test]
        public void Error_Delayed()
        {
            ObservableSource.Concat(true,
                ObservableSource.Range(1, 5),
                ObservableSource.Error<int>(new InvalidOperationException()),
                ObservableSource.Range(11, 5)
            )
            .Test()
            .AssertFailure(typeof(InvalidOperationException), 1, 2, 3, 4, 5, 11, 12, 13, 14, 15);
        }

        [Test]
        public void Null_Source()
        {
            ObservableSource.Concat(
                ObservableSource.Range(1, 5),
                null,
                ObservableSource.Range(11, 5)
            )
            .Test()
            .AssertFailure(typeof(NullReferenceException), 1, 2, 3, 4, 5);
        }

        [Test]
        public void Dispose()
        {
            TestHelper.VerifyDisposeObservableSource<int, int>(o => ObservableSource.Concat(o, o, o));
        }
    }
}
