using NUnit.Framework;
using System;
using System.Collections;
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

            Assert.False(lookup.Contains(1));

            Assert.AreEqual(new List<int>() {  }, lookup[1]);

        }

        [Test]
        public void Enumerate_Generic()
        {
            var lookup = new Lookup<int, int>(EqualityComparer<int>.Default);
            lookup.Add(0, 1);
            lookup.Add(0, 2);

            var list = new List<int>();
            foreach (var g in lookup)
            {
                foreach (var i in g)
                {
                    list.Add(i);
                }
            }

            Assert.AreEqual(new List<int>() { 1, 2 }, list);
        }

        [Test]
        public void Enumerate_NonGeneric()
        {
            var lookup = new Lookup<int, int>(EqualityComparer<int>.Default);
            lookup.Add(0, 1);
            lookup.Add(0, 2);

            var list = new List<object>();
            foreach (var g in (IEnumerable)lookup)
            {
                foreach (var i in (IEnumerable)g)
                {
                    list.Add(i);
                }
            }

            Assert.AreEqual(new List<object>() { 1, 2 }, list);
        }
    }
}
