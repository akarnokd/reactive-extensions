using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Reactive.Linq;

namespace akarnokd.reactive_extensions_test.observablesource
{
    [TestFixture]
    public class ObservableSourceRepeatPredicateTest
    {
        [Test]
        public void Predicate_Plain_Basic()
        {
            var count = 1;
            ObservableSource.Just(1)
                .Repeat(() => count++ < 5)
                .Test()
                .AssertResult(1, 1, 1, 1, 1);
        }

        [Test]
        public void Predicate_Plain_Error()
        {
            var count = 1;
            ObservableSource.Error<int>(new InvalidOperationException())
                .Repeat(() => count++ < 5)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }


        [Test]
        public void Predicate_Plain_Predicate_Crash()
        {
            ObservableSource.Range(1, 5)
                .Repeat(() => { throw new InvalidOperationException(); })
                .Test()
                .AssertFailure(typeof(InvalidOperationException), 1, 2, 3, 4, 5);
        }

        [Test]
        public void Predicate_Plain_Basic_Long()
        {
            var count = 1;
            ObservableSource.Just(1)
                .Repeat(() => count++ < 1000)
                .Test()
                .AssertValueCount(1000)
                .AssertNoError()
                .AssertCompleted();
        }

        [Test]
        public void Predicate_Counted_Basic()
        {
            ObservableSource.Just(1)
                .Repeat(count => count < 5)
                .Test()
                .AssertResult(1, 1, 1, 1, 1);
        }

        [Test]
        public void Predicate_Counted_Error()
        {
            ObservableSource.Error<int>(new InvalidOperationException())
                .Repeat(count => count < 5)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }


        [Test]
        public void Predicate_Counted_Predicate_Crash()
        {
            ObservableSource.Range(1, 5)
                .Repeat(count => { throw new InvalidOperationException(); })
                .Test()
                .AssertFailure(typeof(InvalidOperationException), 1, 2, 3, 4, 5);
        }

        [Test]
        public void Predicate_Counted_Basic_Long()
        {
            ObservableSource.Just(1)
                .Repeat(count => count < 1000)
                .Test()
                .AssertValueCount(1000)
                .AssertNoError()
                .AssertCompleted();
        }

        [Test]
        public void Counted_Basic()
        {
            ObservableSource.Just(1)
                .Repeat(5)
                .Test()
                .AssertResult(1, 1, 1, 1, 1);
        }

        [Test]
        public void Counted_Zero()
        {
            ObservableSource.Just(1)
                .Repeat(0)
                .Test()
                .AssertResult();
        }

        [Test]
        public void Counted_Error()
        {
            ObservableSource.Error<int>(new InvalidOperationException())
                .Repeat(5)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }


        [Test]
        public void Counted_Infinite()
        {
            ObservableSource.Range(1, 5)
                .Repeat()
                .Take(5)
                .Test()
                .AssertResult(1, 2, 3, 4, 5);
        }

        [Test]
        public void Counted_Basic_Long()
        {
            ObservableSource.Just(1)
                .Repeat(1000)
                .Test()
                .AssertValueCount(1000)
                .AssertNoError()
                .AssertCompleted();
        }
    }
}
