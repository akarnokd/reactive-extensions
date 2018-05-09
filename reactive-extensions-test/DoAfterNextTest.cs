using System;
using NUnit.Framework;
using akarnokd.reactive_extensions;
using System.Reactive.Linq;
using System.Collections.Generic;

namespace akarnokd.reactive_extensions_test
{
    [TestFixture]
    public class DoAfterNextTest
    {
        [Test]
        public void Basic()
        {
            var list = new List<int>();

            var up = new UnicastSubject<int>();

            var ts = up.Do(v => { list.Add(v); })
                .DoAfterNext(v => { list.Add(-v); })
                .Test();

            up.EmitAll(1, 2, 3, 4, 5);

            ts.AssertResult(1, 2, 3, 4, 5);

            Assert.AreEqual(new List<int>() { 1, -1, 2, -2, 3, -3, 4, -4, 5, -5 }, list);
        }

        [Test]
        public void Handler_Crash()
        {
            var up = new UnicastSubject<int>();

            var ts = up
                .DoAfterNext(v => { throw new InvalidOperationException(); })
                .Test();

            up.EmitAll(1, 2, 3, 4, 5);

            ts.AssertFailure(typeof(InvalidOperationException), 1);

            Assert.False(up.HasObserver());
        }

        [Test]
        public void Basic_Alternative()
        {
            var list = new List<int>();

            var up = new UnicastSubject<int>();

            var ts = up
                .DoAfterNext(v => { list.Add(-v); })
                .Do(v => { list.Add(v); })
                .Test();

            up.EmitAll(1, 2, 3, 4, 5);

            ts.AssertResult(1, 2, 3, 4, 5);

            Assert.AreEqual(new List<int>() { 1, -1, 2, -2, 3, -3, 4, -4, 5, -5 }, list);
        }
    }
}
