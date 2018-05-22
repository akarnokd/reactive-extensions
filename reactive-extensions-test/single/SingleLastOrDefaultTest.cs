using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Reactive.Linq;

namespace akarnokd.reactive_extensions_test.single
{
    [TestFixture]
    public class SingleLastOrDefaultTest
    {
        [Test]
        public void Last()
        {
            Observable.Range(1, 5)
                .LastOrDefault(-100)
                .Test()
                .AssertResult(5);
        }

        [Test]
        public void Empty()
        {
            Observable.Empty<int>()
                .LastOrDefault(-100)
                .Test()
                .AssertResult(-100);
        }

        [Test]
        public void Error()
        {
            Observable.Throw<int>(new InvalidOperationException())
                .LastOrDefault(-100)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }
    }
}
