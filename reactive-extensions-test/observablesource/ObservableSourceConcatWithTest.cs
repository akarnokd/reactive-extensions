using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.observablesource
{
    [TestFixture]
    public class ObservableSourceConcatWithTest
    {
        [Test]
        public void Basic()
        {
            ObservableSource.Range(1, 5).Concat(ObservableSource.Range(6, 5))
                .Test()
                .AssertResult(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
        }

        [Test]
        public void Error_First()
        {
            var count = 0;

            ObservableSource.Error<int>(new InvalidOperationException())
                .Concat(ObservableSource.FromFunc(() => ++count))
                .Test()
                .AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(0, count);
        }

        [Test]
        public void Error_Second()
        {
            var count = 0;

            ObservableSource.Empty<int>()
                .Concat(ObservableSource.Error<int>(new InvalidOperationException()))
                .Test()
                .AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(0, count);
        }

        [Test]
        public void Dispose_First()
        {
            TestHelper.VerifyDisposeObservableSource<int, int>(o => o.Concat(ObservableSource.Never<int>()));
        }

        [Test]
        public void Dispose_Second()
        {
            TestHelper.VerifyDisposeObservableSource<int, int>(o => ObservableSource.Empty<int>().Concat(o));
        }
    }
}
