using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Reactive.Linq;

namespace akarnokd.reactive_extensions_test
{
    [TestFixture]
    public class TakeUntilPredicatTest
    {
        [Test]
        public void Basic()
        {
            Observable.Range(1, 5)
                 .TakeUntil(v => false)
                 .Test()
                 .AssertResult(1, 2, 3, 4, 5);
        }

        [Test]
        public void Basic_Stop_Asap()
        {
            Observable.Range(1, 5)
                 .TakeUntil(v => true)
                 .Test()
                 .AssertResult(1);
        }

        [Test]
        public void Until_Some_Value()
        {
            Observable.Range(1, 5)
                 .TakeUntil(v => v == 3)
                 .Test()
                 .AssertResult(1, 2, 3);
        }

        [Test]
        public void Error()
        {
            Observable.Range(1, 5).ConcatError(new InvalidOperationException())
                 .TakeUntil(v => false)
                 .Test()
                 .AssertFailure(typeof(InvalidOperationException), 1, 2, 3, 4, 5);
        }

        [Test]
        public void StopPredicate_Crash()
        {
            Observable.Range(1, 5)
                 .TakeUntil(v => {
                     if (v == 3)
                     {
                         throw new InvalidOperationException();
                     }
                     return false;
                 })
                 .Test()
                 .AssertFailure(typeof(InvalidOperationException), 1, 2, 3);
        }
    }
}
