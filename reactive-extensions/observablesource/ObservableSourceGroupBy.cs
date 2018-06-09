using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    internal sealed class ObservableSourceGroupBy<T, K, V> : IObservableSource<IGroupedObservableSource<K, V>>
    {
        readonly IObservableSource<T> source;

        readonly Func<T, K> keySelector;

        readonly Func<T, V> valueSelector;

        readonly IEqualityComparer<K> keyComparer;

        public ObservableSourceGroupBy(IObservableSource<T> source, Func<T, K> keySelector, Func<T, V> valueSelector, IEqualityComparer<K> keyComparer)
        {
            this.source = source;
            this.keySelector = keySelector;
            this.valueSelector = valueSelector;
            this.keyComparer = keyComparer;
        }

        public void Subscribe(ISignalObserver<IGroupedObservableSource<K, V>> observer)
        {
            source.Subscribe(new GroupByObserver(observer, keySelector, valueSelector, keyComparer));
        }

        sealed class GroupByObserver : ISignalObserver<T>, IDisposable
        {
            readonly ISignalObserver<IGroupedObservableSource<K, V>> downstream;

            readonly Func<T, K> keySelector;

            readonly Func<T, V> valueSelector;

            readonly ConcurrentDictionary<K, Group> groups;

            IDisposable upstream;

            int active;

            int once;

            bool done;

            public GroupByObserver(ISignalObserver<IGroupedObservableSource<K, V>> downstream, Func<T, K> keySelector, Func<T, V> valueSelector, IEqualityComparer<K> keyComparer)
            {
                this.downstream = downstream;
                this.keySelector = keySelector;
                this.valueSelector = valueSelector;
                this.groups = new ConcurrentDictionary<K, Group>(keyComparer);
                Volatile.Write(ref active, 1);
            }

            public void Dispose()
            {
                if (Interlocked.CompareExchange(ref once, 1, 0) == 0)
                {
                    if (Interlocked.Decrement(ref active) == 0)
                    {
                        upstream.Dispose();
                    }
                }
            }

            public void OnCompleted()
            {
                if (!done)
                {
                    Volatile.Write(ref done, true);
                    foreach (var g in groups)
                    {
                        g.Value.OnCompleted();
                    }
                    groups.Clear();
                    downstream.OnCompleted();
                }
            }

            public void OnError(Exception ex)
            {
                if (!done)
                {
                    Volatile.Write(ref done, true);
                    foreach (var g in groups)
                    {
                        g.Value.OnError(ex);
                    }
                    groups.Clear();
                    downstream.OnError(ex);
                }
            }

            public void OnNext(T item)
            {
                if (done)
                {
                    return;
                }

                var key = default(K);
                var value = default(V);

                try
                {
                    key = keySelector(item);
                    value = valueSelector(item);
                }
                catch (Exception ex)
                {
                    upstream.Dispose();
                    OnError(ex);
                    return;
                }

                if (!groups.TryGetValue(key, out var g))
                {
                    if (Volatile.Read(ref once) == 0)
                    {
                        g = new Group(key, this);
                        groups.TryAdd(key, g);
                        Interlocked.Increment(ref active);
                        downstream.OnNext(g);
                    }
                }
                g.OnNext(value);
            }

            public void OnSubscribe(IDisposable d)
            {
                upstream = d;
                downstream.OnSubscribe(this);
            }

            void GroupDone(K key)
            {
                if (!Volatile.Read(ref done))
                {
                    groups.TryRemove(key, out var _);
                    if (Interlocked.Decrement(ref active) == 0)
                    {
                        upstream.Dispose();
                    }
                }
            }

            sealed class Group : IGroupedObservableSource<K, V>
            {
                readonly K key;

                readonly GroupByObserver parent;

                readonly MonocastSubject<V> subject;

                public Group(K key, GroupByObserver parent)
                {
                    this.key = key;
                    this.parent = parent;
                    this.subject = new MonocastSubject<V>(onTerminate: () => parent.GroupDone(key));
                }

                public K Key => key;

                public void Subscribe(ISignalObserver<V> observer)
                {
                    subject.Subscribe(observer);
                }
                
                public void OnNext(V item)
                {
                    subject.OnNext(item);
                }

                public void OnError(Exception ex)
                {
                    subject.OnError(ex);
                }

                public void OnCompleted()
                {
                    subject.OnCompleted();
                }
            }
        }
    }
}
