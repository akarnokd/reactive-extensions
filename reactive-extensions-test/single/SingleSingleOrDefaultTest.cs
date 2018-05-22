using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Reactive.Linq;

namespace akarnokd.reactive_extensions_test.single
{
    [TestFixture]
    public class SingleSingleOrDefaultTest
    {
        [Test]
        public void Empty()
        {
            Observable.Empty<int>()
                .SingleOrDefault(-100)
                .Test()
                .AssertResult(-100);
        }

        [Test]
        public void Just()
        {
            Observable.Return(1)
                .SingleOrDefault(-100)
                .Test()
                .AssertResult(1);
        }

        [Test]
        public void Range()
        {
            Observable.Range(1, 5)
                .SingleOrDefault(-100)
                .Test()
                .AssertFailure(typeof(IndexOutOfRangeException));
        }

        [Test]
        public void Error()
        {
            Observable.Throw<int>(new InvalidOperationException())
                .SingleOrDefault(-100)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Error_Last()
        {
            Observable.Return(1).ConcatError(new InvalidOperationException())
                .SingleOrDefault(-100)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }
    }
}
