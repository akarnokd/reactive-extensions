using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Collections.Generic;

namespace akarnokd.reactive_extensions_test
{
    [TestFixture]
    public class LookupTest
    {
        [Test]
        public void Basic()
        {
            var lookup = new Lookup<int, int>(EqualityComparer<int>.Default);

            Assert.AreEqual(0, lookup.Count);

            Assert.False(lookup.Contains(0));

            lookup.Add(0, 1);

            Assert.True(lookup.Contains(0));

            Assert.AreEqual(1, lookup.Count);

            lookup.Add(0, 2);

            Assert.AreEqual(1, lookup.Count);

            Assert.AreEqual(new List<int>() { 1, 2 }, lookup[0]);
        }
    }
}
