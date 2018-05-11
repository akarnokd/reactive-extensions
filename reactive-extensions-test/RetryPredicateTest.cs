using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Reactive.Linq;
using System.Reflection;

namespace akarnokd.reactive_extensions_test
{
    [TestFixture]
    public class RetryPredicateTest
    {
        [Test]
        public void Basic()
        {
            Observable.Range(1, 5)
                 .Retry((e, c) => true)
                 .Test()
                 .AssertResult(1, 2, 3, 4, 5);
        }

        [Test]
        public void Basic_Wrong_Error()
        {
            Observable.Range(1, 5).Concat(Observable.Throw<int>(new NotImplementedException()))
                 .Retry((e, c) => typeof(InvalidOperationException).GetTypeInfo().IsAssignableFrom(e.GetType().GetTypeInfo()))
                 .Test()
                 .AssertFailure(typeof(NotImplementedException), 1, 2, 3, 4, 5);
        }
    }
}
