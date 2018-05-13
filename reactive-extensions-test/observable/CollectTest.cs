using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Reactive.Linq;
using System.Collections.Generic;

namespace akarnokd.reactive_extensions_test.observable
{
    [TestFixture]
    public class CollectTest
    {
        [Test]
        public void Basic()
        {
            Observable.Range(1, 5)
                 .Collect(() => new List<int>(), (a, b) => a.Add(b))
                 .Test()
                 .AssertResult(new List<int>() { 1, 2, 3, 4, 5 });
        }

        [Test]
        public void Error()
        {
            Observable.Throw<int>(new InvalidOperationException())
                 .Collect(() => new List<int>(), (a, b) => a.Add(b))
                 .Test()
                 .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Items_And_Error()
        {
            Observable.Range(1, 5).Concat(
                    Observable.Throw<int>(new InvalidOperationException())
                 )
                 .Collect(() => new List<int>(), (a, b) => a.Add(b))
                 .Test()
                 .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Collection_Supplier_Crash()
        {
            Observable.Range(1, 5)
                 .Collect<int, List<int>>(() => { throw new InvalidOperationException(); }, (a, b) => a.Add(b))
                 .Test()
                 .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Collector_Crash()
        {
            Observable.Range(1, 5)
                 .Collect(() => new List<int>(), (a, b) => {
                     if (b == 3)
                     {
                         throw new InvalidOperationException();
                     }
                     a.Add(b);
                 })
                 .Test()
                 .AssertFailure(typeof(InvalidOperationException));
        }
    }
}
