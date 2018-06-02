using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.observablesource
{
    [TestFixture]
    public class ObservableSourceSkipTest
    {
        [Test]
        public void Basic()
        {
            ObservableSource.Range(1, 5)
                .Skip(3)
                .Test()
                .AssertResult(4, 5);
        }

        [Test]
        public void Double()
        {
            ObservableSource.Range(1, 7)
                .Skip(2)
                .Skip(3)
                .Test()
                .AssertResult(6, 7);
        }

        [Test]
        public void Take()
        {
            ObservableSource.Range(1, 7)
                .Skip(2)
                .Take(3)
                .Test()
                .AssertResult(3, 4, 5);
        }

        [Test]
        public void Zero()
        {
            ObservableSource.Range(1, 5)
                .Skip(0)
                .Test()
                .AssertResult(1, 2, 3, 4, 5);
        }

        [Test]
        public void Negative()
        {
            ObservableSource.Range(1, 5)
                .Skip(-5)
                .Test()
                .AssertResult(1, 2, 3, 4, 5);
        }


        [Test]
        public void More()
        {
            ObservableSource.Range(1, 5)
                .Skip(6)
                .Test()
                .AssertResult();
        }

        [Test]
        public void Error()
        {
            ObservableSource.Error<int>(new InvalidOperationException())
                .Skip(1)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Empty()
        {
            ObservableSource.Empty<int>()
                .Skip(1)
                .Test()
                .AssertResult();
        }
    }
}
