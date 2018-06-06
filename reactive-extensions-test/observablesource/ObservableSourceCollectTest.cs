using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Collections.Generic;
using System.Linq;

namespace akarnokd.reactive_extensions_test.observablesource
{
    [TestFixture]
    public class ObservableSourceCollectTest
    {
        [Test]
        public void Basic()
        {
            ObservableSource.Range(1, 5)
                .Collect(() => new List<int>(), (a, b) => a.Add(b))
                .Test()
                .AssertResult(new List<int>() { 1, 2, 3, 4, 5 });
        }

        [Test]
        public void Error()
        {
            ObservableSource.Error<int>(new InvalidOperationException())
                .Collect(() => new List<int>(), (a, b) => a.Add(b))
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Supplier_Crash()
        {
            ObservableSource.Just(1)
                .Collect<int, List<int>>(() => throw new InvalidOperationException(), (a, b) => a.Add(b))
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Collector_Crash()
        {
            ObservableSource.Range(1, 5)
                .Collect(() => new List<int>(), (a, b) => throw new InvalidOperationException())
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Fused()
        {
            ObservableSource.Range(1, 5)
                .Collect(() => new List<int>(), (a, b) => a.Add(b))
                .Test(fusionMode: FusionSupport.Any)
                .AssertFuseable()
                .AssertFusionMode(FusionSupport.Async)
                .AssertResult(new List<int>() { 1, 2, 3, 4, 5 });
        }

        [Test]
        public void Dispose()
        {
            TestHelper.VerifyDisposeObservableSource<int, List<int>>(m => m.Collect(() => new List<int>(), (a, b) => a.Add(b)));
        }

        [Test]
        public void ToList()
        {
            ObservableSource.Range(1, 5)
                .ToList()
                .Test()
                .AssertResult(new List<int>() { 1, 2, 3, 4, 5 });
        }

        [Test]
        public void ToDictionary()
        {
            var to = ObservableSource.Range(1, 5)
                .ToDictionary(v => v)
                .Test()
                .AssertSubscribed()
                .AssertValueCount(1)
                .AssertNoError()
                .AssertCompleted();

            var dict = to.Items[0];


            for (int i = 1; i <= 5; i++)
            {
                Assert.AreEqual(i, dict[i]);
            }
        }

        [Test]
        public void ToDictionary_ValueSelector()
        {
            var to = ObservableSource.Range(1, 5)
                .ToDictionary(v => v, v => v + 1)
                .Test()
                .AssertSubscribed()
                .AssertValueCount(1)
                .AssertNoError()
                .AssertCompleted();

            var dict = to.Items[0];

            for (int i = 1; i <= 5; i++)
            {
                Assert.AreEqual(i + 1, dict[i]);
            }
        }

        [Test]
        public void ToLoolup()
        {
            var to = ObservableSource.Range(1, 5)
                .ToLookup(v => v % 2)
                .Test()
                .AssertSubscribed()
                .AssertValueCount(1)
                .AssertNoError()
                .AssertCompleted();

            var dict = to.Items[0];

            Assert.AreEqual(new List<int>() { 2, 4 }, dict[0]);
            Assert.AreEqual(new List<int>() { 1, 3, 5 }, dict[1]);
        }

        [Test]
        public void ToLoolup_ValueSelector()
        {
            var to = ObservableSource.Range(1, 5)
                .ToLookup(v => v % 2, v => v + 1)
                .Test()
                .AssertSubscribed()
                .AssertValueCount(1)
                .AssertNoError()
                .AssertCompleted();

            var dict = to.Items[0];

            Assert.AreEqual(new List<int>() { 3, 5 }, dict[0]);
            Assert.AreEqual(new List<int>() { 2, 4, 6 }, dict[1]);
        }
    }
}
