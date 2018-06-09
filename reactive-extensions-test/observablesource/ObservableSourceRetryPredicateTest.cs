using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Reactive.Linq;
using System.Reflection;

namespace akarnokd.reactive_extensions_test.observablesource
{
    [TestFixture]
    public class ObservableSourceRetryPredicateTest
    {
        [Test]
        public void Predicate_Basic()
        {
            ObservableSource.Range(1, 5)
                 .Retry((e, c) => true)
                 .Test()
                 .AssertResult(1, 2, 3, 4, 5);
        }

        [Test]
        public void Predicate_Basic_Wrong_Error()
        {
            ObservableSource.Range(1, 5).Concat(ObservableSource.Error<int>(new NotImplementedException()))
                 .Retry((e, c) => typeof(InvalidOperationException).IsAssignableFrom(e))
                 .Test()
                 .AssertFailure(typeof(NotImplementedException), 1, 2, 3, 4, 5);
        }


        [Test]
        public void Predicate_Basic_Retry_Twice()
        {
            ObservableSource.Range(1, 5).Concat(ObservableSource.Error<int>(new NotImplementedException()))
                 .Retry((e, c) => c != 2)
                 .Test()
                 .AssertFailure(typeof(NotImplementedException), 1, 2, 3, 4, 5, 1, 2, 3, 4, 5);
        }

        [Test]
        public void Predicate_Predicate_Crash()
        {
            var to = ObservableSource.Range(1, 5).Concat(ObservableSource.Error<int>(new NotImplementedException()))
                 .Retry((e, c) => { throw new InvalidOperationException(); })
                 .Test()
                 .AssertFailure(typeof(AggregateException), 1, 2, 3, 4, 5)
                 .AssertCompositeError(0, typeof(NotImplementedException))
                 .AssertCompositeError(1, typeof(InvalidOperationException));
        }

        [Test]
        public void Counted_Basic()
        {
            ObservableSource.Range(1, 5)
                 .Retry()
                 .Test()
                 .AssertResult(1, 2, 3, 4, 5);
        }

        [Test]
        public void Counted_Basic_Times()
        {
            ObservableSource.Range(1, 5)
                 .Retry(5)
                 .Test()
                 .AssertResult(1, 2, 3, 4, 5);
        }

        [Test]
        public void Counted_Basic_Retry_Zero()
        {
            ObservableSource.Range(1, 5).Concat(ObservableSource.Error<int>(new NotImplementedException()))
                 .Retry(0)
                 .Test()
                 .AssertFailure(typeof(NotImplementedException), 1, 2, 3, 4, 5);
        }

        [Test]
        public void Counted_Basic_Retry_One()
        {
            ObservableSource.Range(1, 5).Concat(ObservableSource.Error<int>(new NotImplementedException()))
                 .Retry(1)
                 .Test()
                 .AssertFailure(typeof(NotImplementedException), 1, 2, 3, 4, 5, 1, 2, 3, 4, 5);
        }

        [Test]
        public void Counted_Basic_Retry_Twice()
        {
            ObservableSource.Range(1, 5).Concat(ObservableSource.Error<int>(new NotImplementedException()))
                 .Retry(2)
                 .Test()
                 .AssertFailure(typeof(NotImplementedException), 1, 2, 3, 4, 5, 1, 2, 3, 4, 5, 1, 2, 3, 4, 5);
        }

        [Test]
        public void Counted_Retry_Long()
        {
            var to = ObservableSource.Range(1, 5).Concat(ObservableSource.Error<int>(new NotImplementedException()))
                 .Retry(1000)
                 .Test()
                 .AssertValueCount(5005)
                 .AssertNotCompleted()
                 .AssertError(typeof(NotImplementedException))
                 ;
        }
    }
}
