using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.observablesource
{
    [TestFixture]
    public class ObservableSourceTakeTest
    {
        [Test]
        public void Basic()
        {
            ObservableSource.Range(1, 5)
                .Take(3)
                .Test()
                .AssertResult(1, 2, 3);
        }

        [Test]
        public void Double()
        {
            ObservableSource.Range(1, 5)
                .Take(3)
                .Take(2)
                .Test()
                .AssertResult(1, 2);
        }

        [Test]
        public void Zero()
        {
            ObservableSource.Range(1, 5)
                .Take(0)
                .Test()
                .AssertResult();
        }

        [Test]
        public void Exact()
        {
            ObservableSource.Range(1, 5)
                .Take(5)
                .Test()
                .AssertResult(1, 2, 3, 4, 5);
        }

        [Test]
        public void Error()
        {
            ObservableSource.Error<int>(new InvalidOperationException())
                .Take(3)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Error_Take_Zero()
        {
            ObservableSource.Error<int>(new InvalidOperationException())
                .Take(0)
                .Test()
                .AssertResult();
        }
    }
}
