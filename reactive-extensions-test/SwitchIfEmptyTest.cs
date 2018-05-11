using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Reactive.Linq;

namespace akarnokd.reactive_extensions_test
{
    [TestFixture]
    public class SwitchIfEmptyTest
    {
        [Test]
        public void Basic()
        {
            Observable.Range(1, 5)
                 .SwitchIfEmpty(Observable.Range(6, 5))
                 .Test()
                 .AssertResult(1, 2, 3, 4, 5);
        }

        [Test]
        public void Error()
        {
            Observable.Range(1, 5).ConcatError(new InvalidOperationException())
                 .SwitchIfEmpty(Observable.Range(6, 5))
                 .Test()
                 .AssertFailure(typeof(InvalidOperationException), 1, 2, 3, 4, 5);
        }

        [Test]
        public void Fallback_Error()
        {
            Observable.Empty<int>()
                 .SwitchIfEmpty(Observable.Range(6, 5).ConcatError(new InvalidOperationException()))
                 .Test()
                 .AssertFailure(typeof(InvalidOperationException), 6, 7, 8, 9, 10);
        }

        [Test]
        public void Switch_Null_Fallback()
        {
            Observable.Empty<int>()
                 .SwitchIfEmpty((IObservable<int>)null)
                 .Test()
                 .AssertFailure(typeof(NullReferenceException));
        }

        [Test]
        public void Switch()
        {
            Observable.Empty<int>()
                 .SwitchIfEmpty(Observable.Range(6, 5))
                 .Test()
                 .AssertResult(6, 7, 8, 9, 10);
        }

        [Test]
        public void Switch_Multiple()
        {
            Observable.Empty<int>()
                 .SwitchIfEmpty(Observable.Empty<int>(), Observable.Range(6, 5))
                 .Test()
                 .AssertResult(6, 7, 8, 9, 10);
        }
    }
}
