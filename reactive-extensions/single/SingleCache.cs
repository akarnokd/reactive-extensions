using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Cache the terminal signal of the upstream
    /// and relay/replay it to current or future
    /// single observers.
    /// </summary>
    /// <typeparam name="T">The success value type.</typeparam>
    /// <remarks>Since 0.0.12</remarks>
    internal sealed class SingleCache<T> : ISingleSource<T>, ISingleObserver<T>, IDisposable
    {
        ISingleSource<T> source;

        Action<IDisposable> cancel;

        CacheDisposable[] observers;

        static readonly CacheDisposable[] EMPTY = new CacheDisposable[0];

        static readonly CacheDisposable[] TERMINATED = new CacheDisposable[0];

        T value;

        Exception error;

        IDisposable upstream;

        int once;

        public SingleCache(ISingleSource<T> source, Action<IDisposable> cancel)
        {
            this.source = source;
            this.cancel = cancel;
            Volatile.Write(ref observers, EMPTY);
        }

        public void Dispose()
        {
            DisposableHelper.Dispose(ref upstream);
            OnError(new OperationCanceledException());
        }

        public void OnError(Exception error)
        {
            if (Interlocked.CompareExchange(ref once, 1, 0) == 0)
            {
                this.error = error;
                foreach (var inner in Interlocked.Exchange(ref observers, TERMINATED))
                {
                    if (!inner.IsDisposed())
                    {
                        inner.dowstream.OnError(error);
                    }
                }
                DisposableHelper.WeakDispose(ref upstream);
            }
        }

        public void OnSubscribe(IDisposable d)
        {
            DisposableHelper.SetOnce(ref upstream, d);
        }

        public void OnSuccess(T item)
        {
            if (Interlocked.CompareExchange(ref once, 1, 0) == 0)
            {
                value = item;
                foreach (var inner in Interlocked.Exchange(ref observers, TERMINATED))
                {
                    if (!inner.IsDisposed())
                    {
                        inner.dowstream.OnSuccess(item);
                    }
                }
                DisposableHelper.WeakDispose(ref upstream);
            }
        }

        public void Subscribe(ISingleObserver<T> observer)
        {
            var parent = new CacheDisposable(observer, this);
            observer.OnSubscribe(parent);

            if (Add(parent))
            {
                if (parent.IsDisposed())
                {
                    Remove(parent);
                }
            }
            else
            {
                if (!parent.IsDisposed())
                {
                    var ex = error;
                    if (ex != null)
                    {
                        observer.OnError(ex);
                    }
                    else
                    {
                        observer.OnSuccess(value);
                    }
                }
                return;
            }

            var src = Volatile.Read(ref source);
            if (src != null && Interlocked.CompareExchange(ref source, null, src) == src)
            {
                var c = cancel;
                cancel = null;

                try
                {
                    c?.Invoke(this);
                }
                catch (Exception ex)
                {
                    OnError(ex);
                    return;
                }

                src.Subscribe(this);
            }
        }

        bool Add(CacheDisposable inner)
        {
            for (; ; )
            {
                var a = Volatile.Read(ref observers);
                if (a == TERMINATED)
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

                var j = Array.IndexOf(a, inner);

                if (j < 0)
                {
                    break;
                }

                var b = default(CacheDisposable[]);
                if (n == 1)
                {
                    b = EMPTY;
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

        sealed class CacheDisposable : IDisposable
        {
            internal readonly ISingleObserver<T> dowstream;

            SingleCache<T> parent;

            public CacheDisposable(ISingleObserver<T> dowstrean, SingleCache<T> parent)
            {
                this.dowstream = dowstrean;
                Volatile.Write(ref this.parent, parent);
            }

            public void Dispose()
            {
                Interlocked.Exchange(ref parent, null)?.Remove(this);
            }

            internal bool IsDisposed()
            {
                return Volatile.Read(ref parent) == null;
            }
        }
    }
}
