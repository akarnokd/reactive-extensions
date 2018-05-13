using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Reactive.Linq;

namespace akarnokd.reactive_extensions_test.observable
{
    [TestFixture]
    public class RepeatPredicateTest
    {
        [Test]
        public void Basic()
        {
            var count = 1;
            Observable.Return(1)
                .Repeat(() => count++ < 5)
                .Test()
                .AssertResult(1, 1, 1, 1, 1);
        }

        [Test]
        public void Error()
        {
            var count = 1;
            Observable.Throw<int>(new InvalidOperationException())
                .Repeat(() => count++ < 5)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }


        [Test]
        public void Predicate_Crash()
        {
            Observable.Range(1, 5)
                .Repeat(() => { throw new InvalidOperationException(); })
                .Test()
                .AssertFailure(typeof(InvalidOperationException), 1, 2, 3, 4, 5);
        }

        [Test]
        public void Basic_Long()
        {
            var count = 1;
            Observable.Return(1)
                .Repeat(() => count++ < 1000)
                .Test()
                .AssertValueCount(1000)
                .AssertNoError()
                .AssertCompleted();
        }
    }
}
