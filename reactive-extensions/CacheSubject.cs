using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Caches some or all items (with optional time retention policy)
    /// and relays/replays the events to current or future observers.
    /// </summary>
    /// <typeparam name="T">The element type of the sequence.</typeparam>
    /// <remarks>Since 0.0.22</remarks>
    public sealed class CacheSubject<T> : IObservableSubject<T>, ISubjectExtensions, IDisposable
    {
        readonly bool refCount;

        readonly ICacheManager manager;

        CacheDisposable[] observers;

        IDisposable upstream;

        int once;

        static readonly CacheDisposable[] Empty = new CacheDisposable[0];
        static readonly CacheDisposable[] Terminated = new CacheDisposable[0];

        static readonly IDisposable Prepared = new BooleanDisposable();

        /// <summary>
        /// Returns true if this subject has any observers.
        /// </summary>
        public bool HasObservers => HasObserver();

        /// <summary>
        /// Constructs a fresh, all-caching CacheSubject.
        /// </summary>
        /// <param name="refCount">If true, the last observer will also dispose
        /// the upstream connection and further observers receive a terminal event.</param>
        /// <param name="capacityHint">The number of items expected (affects resizing policies)</param>
        public CacheSubject(bool refCount = false, int capacityHint = 16)
        {
            this.refCount = refCount;
            this.manager = new CacheAllManager(capacityHint);
            Volatile.Write(ref observers, Empty);
        }

        /// <summary>
        /// Optional call: set the upstream disposable
        /// to later dispose via <see cref="Dispose"/>.
        /// </summary>
        /// <param name="d">The upstream disposable.</param>
        public void OnSubscribe(IDisposable d)
        {
            for (; ; )
            {
                var curr = Volatile.Read(ref upstream);
                if (curr != null && curr != Prepared)
                {
                    d?.Dispose();
                    return;
                }
                if (Interlocked.CompareExchange(ref upstream, d, curr) == curr)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Prepare this subject by trying to set the
        /// prepared indicator (ignored by OnSubscribe)
        /// and return true if succeeded.
        /// </summary>
        /// <returns>True if the prepared instance could be swapped in</returns>
        internal bool Prepare()
        {
            return Interlocked.CompareExchange(ref upstream, Prepared, null) == null;
        }

        /// <summary>
        /// Disposes the upstream connection.
        /// </summary>
        public void Dispose()
        {
            DisposableHelper.Dispose(ref upstream);
        }

        /// <summary>
        /// Returns true if this subject has been disposed.
        /// </summary>
        /// <returns>True if this subject has been disposed</returns>
        public bool IsDisposed()
        {
            return DisposableHelper.IsDisposed(ref upstream);
        }

        /// <summary>
        /// Returns the terminal exception if present.
        /// </summary>
        /// <returns>The terminal exception or null if not terminated
        /// or terminated normally.</returns>
        public Exception GetException()
        {
            var ex = manager.Exception();
            if (ex != null && ex != ExceptionHelper.TERMINATED)
            {
                return ex;
            }
            return null;
        }

        /// <summary>
        /// Returns true if this subject has been terminated
        /// via <see cref="OnCompleted"/>.
        /// </summary>
        /// <returns>True if this subject terminated normally.</returns>
        public bool HasCompleted()
        {
            return manager.Exception() == ExceptionHelper.TERMINATED;
        }

        /// <summary>
        /// Returns true if this subject has been
        /// terminated via <see cref="OnError(Exception)"/>.
        /// </summary>
        /// <returns>True if this subject has been terminated with an error.</returns>
        public bool HasException()
        {
            var ex = manager.Exception();
            return ex != null && ex != ExceptionHelper.TERMINATED;
        }

        /// <summary>
        /// Returns true if this subject has observers.
        /// </summary>
        /// <returns>True if this subject has observers</returns>
        public bool HasObserver()
        {
            return Volatile.Read(ref observers).Length != 0;
        }

        /// <summary>
        /// Dispatch an error to all current or late observers.
        /// </summary>
        public void OnCompleted()
        {
            if (Interlocked.CompareExchange(ref once, 1, 0) == 0)
            {
                var manager = this.manager;
                manager.Completed();
                foreach (var inner in Interlocked.Exchange(ref observers, Terminated))
                {
                    manager.Drain(inner);
                }
            }
        }

        /// <summary>
        /// Dispatch an error to all current or
        /// late observers.
        /// </summary>
        /// <param name="ex">The error to dispatch or retain.</param>
        public void OnError(Exception ex)
        {
            ValidationHelper.RequireNonNullRef(ex, nameof(ex));
            if (Interlocked.CompareExchange(ref once, 1, 0) == 0)
            {
                var manager = this.manager;
                manager.Error(ex);
                foreach (var inner in Interlocked.Exchange(ref observers, Terminated))
                {
                    manager.Drain(inner);
                }
            }
        }

        /// <summary>
        /// Dispatch an item to all current observers.
        /// </summary>
        /// <param name="item">The item to dispatch.</param>
        public void OnNext(T item)
        {
            var manager = this.manager;
            manager.Next(item);
            foreach (var inner in Volatile.Read(ref observers))
            {
                manager.Drain(inner);
            }
        }

        /// <summary>
        /// Subscribes the observer to this subject and
        /// relays the live items or replays the terminal
        /// event to it.
        /// </summary>
        /// <param name="observer">The observer to dispatch signals to.</param>
        public void Subscribe(ISignalObserver<T> observer)
        {
            var parent = new CacheDisposable(observer, this);
            observer.OnSubscribe(parent);

            Add(parent);
            if (parent.IsDisposed())
            {
                Remove(parent);
                return;
            }
            manager.Drain(parent);
        }

        bool Add(CacheDisposable inner)
        {
            for (; ; )
            {
                var a = Volatile.Read(ref observers);
                if (a == Terminated)
                {
                    return false;
                }
                var n = a.Length;
                var b = new CacheDisposable[n + 1];
                Array.Copy(a, 0, b, 0, n);
                b[n] = inner;
                if (Interlocked.CompareExchange(ref observers, b, a) == a)
                {
                    return true;
                }
            }
        }

        void Remove(CacheDisposable inner)
        {
            for (; ; )
            {
                var a = Volatile.Read(ref observers);
                var n = a.Length;
                if (n == 0)
                {
                    break;
                }
                int j = Array.IndexOf(a, inner);
                if (j < 0)
                {
                    break;
                }
                var b = default(CacheDisposable[]);

                if (n == 1)
                {
                    if (refCount)
                    {
                        if (Interlocked.CompareExchange(ref observers, Terminated, a) == a)
                        {
                            DisposableHelper.Dispose(ref upstream);
                            break;
                        }
                        continue;
                    }

                    b = Empty;
                }
                else
                {
                    b = new CacheDisposable[n - 1];
                    Array.Copy(a, 0, b, 0, j);
                    Array.Copy(a, j + 1, b, j, n - j - 1);
                }
                if (Interlocked.CompareExchange(ref observers, b, a) == a)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// The common API for caching up items.
        /// </summary>
        internal interface ICacheManager
        {
            void Next(T item);

            void Error(Exception ex);

            void Completed();

            void Drain(CacheDisposable cd);

            Exception Exception();
        }

        internal sealed class CacheAllManager : ICacheManager
        {
            readonly Node head;

            readonly int capacity;

            Node tail;

            Exception error;

            long size;

            int tailOffset;


            internal CacheAllManager(int capacityHint)
            {
                this.capacity = capacityHint;
                var n = new Node(capacityHint);
                head = n;
                tail = n;
                Interlocked.Exchange(ref size, 0);
            }

            public void Completed()
            {
                Volatile.Write(ref error, ExceptionHelper.TERMINATED);
            }

            public void Drain(CacheDisposable cd)
            {
                if (Interlocked.Increment(ref cd.wip) != 1)
                {
                    return;
                }

                var missed = 1;

                for (; ; )
                {
                    if (cd.IsDisposed())
                    {
                        cd.node = null;
                    }
                    else
                    {
                        var ex = Volatile.Read(ref error);
                        var available = Volatile.Read(ref size);
                        var idx = cd.index;

                        if (ex != null && available == idx)
                        {
                            cd.node = null;
                            if (ex == ExceptionHelper.TERMINATED)
                            {
                                cd.OnCompleted();
                            }
                            else
                            {
                                cd.OnError(ex);
                            }
                            cd.ClearRefs();
                        }
                        else
                        if (available != idx)
                        {
                            var n = (Node)cd.node;
                            if (n == null)
                            {
                                n = head;
                                cd.node = n;
                            }

                            var offs = cd.offset;
                            if (offs == capacity)
                            {
                                n = Volatile.Read(ref n.next);
                                cd.node = n;
                                offs = 0;
                                cd.offset = 1;
                            }
                            else
                            {
                                cd.offset = offs + 1;
                            }
                            cd.index = idx + 1;

                            cd.OnNext(n.array[offs]);
                            continue;
                        }
                    }

                    missed = Interlocked.Add(ref cd.wip, -missed);
                    if (missed == 0)
                    {
                        break;
                    }
                }
            }

            public void Error(Exception ex)
            {
                Volatile.Write(ref error, ex);
            }

            public Exception Exception()
            {
                return Volatile.Read(ref error);
            }

            public void Next(T item)
            {
                var t = tail;

                var to = tailOffset;
                if (to == capacity)
                {
                    var u = new Node(capacity);

                    u.array[0] = item;
                    tailOffset = 1;
                    tail = u;
                    Volatile.Write(ref t.next, u);
                }
                else
                {
                    t.array[to] = item;
                    tailOffset = to + 1;
                }

                Interlocked.Exchange(ref size, size + 1);
            }

            sealed class Node
            {
                internal readonly T[] array;

                internal Node next;

                public Node(int capacity)
                {
                    this.array = new T[capacity];
                }
            }
        }

        internal sealed class CacheDisposable : IDisposable
        {
            ISignalObserver<T> downstream;

            CacheSubject<T> parent;

            internal int wip;

            internal long index;

            internal object node;

            internal int offset;

            public CacheDisposable(ISignalObserver<T> downstream, CacheSubject<T> parent)
            {
                this.downstream = downstream;
                Volatile.Write(ref this.parent, parent);
            }

            public void Dispose()
            {
                if (Interlocked.Exchange(ref downstream, null) != null)
                {
                    parent.Remove(this);
                    parent.manager.Drain(this);
                    parent = null;
                }
            }

            internal void OnNext(T item)
            {
                Volatile.Read(ref downstream)?.OnNext(item);
            }

            internal void OnError(Exception ex)
            {
                Volatile.Read(ref downstream)?.OnError(ex);
            }

            internal void OnCompleted()
            {
                Volatile.Read(ref downstream)?.OnCompleted();
            }

            internal bool IsDisposed()
            {
                return Volatile.Read(ref downstream) == null;
            }

            internal void ClearRefs()
            {
                Volatile.Write(ref downstream, null);
                Volatile.Write(ref parent, null);
            }
        }
    }
}
