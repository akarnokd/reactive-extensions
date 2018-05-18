using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Reactive.Linq;
using System.Reactive.Concurrency;

namespace akarnokd.reactive_extensions_test.observable
{
    [TestFixture]
    public class ConcatManyTest
    {
        [Test]
        public void Basic()
        {
            new[]
            {
                Observable.Range(1, 5),
                Observable.Range(6, 5)
            }
            .ToObservable()
            .ConcatMany()
            .Test()
            .AssertResult(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
        }

        [Test]
        public void Take()
        {
            new[]
            {
                Observable.Range(1, 5),
                Observable.Range(6, 5)
            }
            .ToObservable(NewThreadScheduler.Default)
            .ConcatMany()
            .Take(7)
            .Test()
            .AwaitDone(TimeSpan.FromSeconds(5))
            .AssertResult(1, 2, 3, 4, 5, 6, 7);
        }

        [Test]
        public void Error()
        {
            new[]
            {
                Observable.Range(1, 5),
                Observable.Throw<int>(new InvalidOperationException()),
                Observable.Range(6, 5)
            }
            .ToObservable()
            .ConcatMany()
            .Test()
            .AssertFailure(typeof(InvalidOperationException), 1, 2, 3, 4, 5);
        }
    }
}
