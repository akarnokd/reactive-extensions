using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Caches the upstream values and replays it to current
    /// and late observers.
    /// </summary>
    /// <typeparam name="T">The element type of the sequence.</typeparam>
    /// <remarks>Since 0.0.22</remarks>
    internal sealed class ObservableSourceCache<T> : IObservableSource<T>, ISignalObserver<T>, IDisposable
    {
        readonly IObservableSource<T> source;

        readonly Action<IDisposable> cancel;

        CacheConsumerDisposable[] observers;

        static readonly CacheConsumerDisposable[] EMPTY = new CacheConsumerDisposable[0];

        static readonly CacheConsumerDisposable[] TERMINATED = new CacheConsumerDisposable[0];

        IDisposable upstream;

        Exception terminated;

        int offset;
        
        int size;

        readonly Node head;

        Node tail;

        int once;

        public ObservableSourceCache(IObservableSource<T> source, Action<IDisposable> cancel, int capacityHint)
        {
            this.source = source;
            this.cancel = cancel;
            var n = new Node(capacityHint);
            this.tail = n;
            this.head = n;
            Volatile.Write(ref this.observers, EMPTY);
        }

        public void OnSubscribe(IDisposable d)
        {
            DisposableHelper.SetOnce(ref upstream, d);
        }

        public void Subscribe(ISignalObserver<T> observer)
        {
            var ccd = new CacheConsumerDisposable(observer, this, head);
            observer.OnSubscribe(ccd);
            Add(ccd);
            if (ccd.IsDisposed())
            {
                Remove(ccd);
                return;
            }
            if (Volatile.Read(ref once) == 0 && Interlocked.CompareExchange(ref once, 1, 0) == 0)
            {
                cancel?.Invoke(this);

                source.Subscribe(this);
            }
            Drain(ccd);
        }

        bool Add(CacheConsumerDisposable ccd)
        {
            for (; ; )
            {
                var a = Volatile.Read(ref observers);
                if (a == TERMINATED)
                {
                    return false;
                }
                var n = a.Length;
                var b = new CacheConsumerDisposable[n + 1];
                Array.Copy(a, 0, b, 0, n);
                b[n] = ccd;
                if (Interlocked.CompareExchange(ref observers, b, a) == a)
                {
                    return true;
                }
            }
        }

        void Remove(CacheConsumerDisposable ccd)
        {
            for (; ; )
            {
                var a = Volatile.Read(ref observers);
                var n = a.Length;
                if (n == 0)
                {
                    break;
                }

                var j = -1;

                for (int i = 0; i < n; i++)
                {
                    if (a[i] == ccd)
                    {
                        j = i;
                        break;
                    }
                }

                if (j < 0)
                {
                    break;
                }

                var b = default(CacheConsumerDisposable[]);

                if (n == 1)
                {
                    b = EMPTY;
                }
                else
                {
                    b = new CacheConsumerDisposable[n - 1];
                    Array.Copy(a, 0, b, 0, j);
                    Array.Copy(a, j + 1, b, j, n - j - 1);
                }
                if (Interlocked.CompareExchange(ref observers, b, a) == a)
                {
                    break;
                }
            }
        }

        void Drain(CacheConsumerDisposable ccd)
        {
            if (Interlocked.Increment(ref ccd.wip) != 1)
            {
                return;
            }

            var missed = 1;
            var downstream = ccd.downstream;

            for (; ; )
            {
                var i = ccd.index;
                var n = ccd.node;
                var o = ccd.offset;

                if (n != null)
                {
                    var a = n.items;
                    var cap = a.Length;

                    for (; ;)
                    {
                        if (ccd.IsDisposed())
                        {
                            n = null;
                            break;
                        }
                        var ex = Volatile.Read(ref terminated);
                        var s = Volatile.Read(ref size);

                        bool empty = i == s;

                        if (ex != null && empty)
                        {
                            if (ex == ExceptionHelper.TERMINATED)
                            {
                                downstream.OnCompleted();
                            }
                            else
                            {
                                downstream.OnError(ex);
                            }

                            n = null;
                            break;
                        }

                        if (empty)
                        {
                            break;
                        }

                        if (o == cap)
                        {
                            var b = n.next;
                            n = b;
                            a = b.items;
                            o = 0;
                        }

                        var v = a[o];

                        downstream.OnNext(v);

                        i++;
                        o++;
                    }

                    ccd.index = i;
                    ccd.node = n;
                    ccd.offset = o;
                }

                missed = Interlocked.Add(ref ccd.wip, -missed);
                if (missed == 0)
                {
                    break;
                }
            }
        }

        public void OnCompleted()
        {
            Terminate(ExceptionHelper.TERMINATED);
            DisposableHelper.Dispose(ref upstream);
        }

        public void OnError(Exception error)
        {
            Terminate(error);
            DisposableHelper.Dispose(ref upstream);
        }

        public void OnNext(T value)
        {
            var o = offset;
            var t = this.tail;
            var a = tail.items;
            var cap = a.Length;
            
            if (o == cap)
            {
                var b = new Node(cap);
                b.items[0] = value;
                t.next = b;
                offset = 1;
                tail = b;
            }
            else
            {
                a[o] = value;
                offset = o + 1;
            }

            Interlocked.Increment(ref size);
            foreach (var ccd in Volatile.Read(ref observers))
            {
                Drain(ccd);
            }
        }

        public void Dispose()
        {
            DisposableHelper.Dispose(ref upstream);
            var ex = new OperationCanceledException();
            Terminate(ex);
        }

        void Terminate(Exception ex)
        {
            if (Interlocked.CompareExchange(ref terminated, ex, null) == null)
            {
                foreach (var ccd in Interlocked.Exchange(ref observers, TERMINATED))
                {
                    Drain(ccd);
                }
            }
        }

        internal sealed class Node
        {
            internal T[] items;
            internal Node next;
            internal Node(int capacityHint)
            {
                this.items = new T[capacityHint];
            }
        }

        internal sealed class CacheConsumerDisposable : IDisposable
        {
            internal readonly ISignalObserver<T> downstream;

            ObservableSourceCache<T> parent;

            internal int wip;

            internal int index;

            internal Node node;

            internal int offset;

            public CacheConsumerDisposable(ISignalObserver<T> downstream, ObservableSourceCache<T> parent, Node head)
            {
                this.downstream = downstream;
                Volatile.Write(ref this.parent, parent);
                Volatile.Write(ref this.node, head);
            }

            internal bool IsDisposed()
            {
                return Volatile.Read(ref parent) == null;
            }

            public void Dispose()
            {
                var d = Interlocked.Exchange(ref parent, null);

                if (d != null)
                {
                    d.Remove(this);
                    d.Drain(this);
                }
            }
        }
    }
}
