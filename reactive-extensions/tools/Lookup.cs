using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace akarnokd.reactive_extensions
{
    internal sealed class Lookup<K, V> : ILookup<K, V>
    {
        readonly Dictionary<K, List<V>> dictionary;

        public IEnumerable<V> this[K key]
        {
            get
            {
                if (!dictionary.TryGetValue(key, out var list))
                {
                    return Enumerable.Empty<V>();
                }
                return Yield(list);
            }
        }

        private IEnumerable<V> Yield(List<V> list)
        {
            foreach (var v in list)
            {
                yield return v;
            }
        }

        public int Count => dictionary.Count;

        public Lookup(IEqualityComparer<K> keyComparer)
        {
            this.dictionary = new Dictionary<K, List<V>>(keyComparer);
        }

        public bool Contains(K key)
        {
            return dictionary.ContainsKey(key);
        }

        public IEnumerator<IGrouping<K, V>> GetEnumerator()
        {
            foreach (var kv in dictionary)
            {
                yield return new LookupGrouping(kv);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(K key, V value)
        {
            if (!dictionary.TryGetValue(key, out var list))
            {
                list = new List<V>();
                dictionary.Add(key, list);
            }
            list.Add(value);
        }

        sealed class LookupGrouping : IGrouping<K, V>
        {
            readonly KeyValuePair<K, List<V>> kv;

            public LookupGrouping(KeyValuePair<K, List<V>> kv)
            {
                this.kv = kv;
            }

            public K Key => kv.Key;

            public IEnumerator<V> GetEnumerator()
            {
                foreach (var v in kv.Value)
                {
                    yield return v;
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }
}
