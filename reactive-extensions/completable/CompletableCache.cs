using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using static akarnokd.reactive_extensions.ValidationHelper;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Caches the terminal signal and relays/replays it to current
    /// or late completable observers.
    /// </summary>
    /// <remarks>Since 0.0.10</remarks>
    internal sealed class CompletableCache : ICompletableSource, ICompletableObserver, IDisposable
    {
        static readonly InnerDisposable[] EMPTY = new InnerDisposable[0];

        static readonly InnerDisposable[] TERMINATED = new InnerDisposable[0];

        ICompletableSource source;

        Action<IDisposable> cancel;

        InnerDisposable[] observers;

        IDisposable upstream;

        Exception error;

        public CompletableCache(ICompletableSource source, Action<IDisposable> cancel)
        {
            this.source = source;
            this.cancel = cancel;
            Volatile.Write(ref observers, EMPTY);
        }

        public void Dispose()
        {
            if (DisposableHelper.Dispose(ref upstream))
            {
                OnError(new OperationCanceledException());
            }
        }

        public void OnCompleted()
        {
            if (Interlocked.CompareExchange(ref this.error, ExceptionHelper.TERMINATED, null) == null) {
                foreach (var inner in Interlocked.Exchange(ref observers, TERMINATED))
                {
                    if (!inner.IsDisposed())
                    {
                        inner.downstream.OnCompleted();
                    }
                }
            }
        }

        public void OnError(Exception error)
        {
            if (Interlocked.CompareExchange(ref this.error, error, null) == null)
            {
                foreach (var inner in Interlocked.Exchange(ref observers, TERMINATED))
                {
                    if (!inner.IsDisposed())
                    {
                        inner.downstream.OnError(error);
                    }
                }
            }
        }

        public void OnSubscribe(IDisposable d)
        {
            DisposableHelper.SetOnce(ref upstream, d);
        }

        public void Subscribe(ICompletableObserver observer)
        {
            var inner = new InnerDisposable(observer, this);
            observer.OnSubscribe(inner);

            if (Add(inner))
            {
                if (inner.IsDisposed())
                {
                    Remove(inner);
                }
            }
            else
            {
                if (!inner.IsDisposed())
                {
                    var ex = Volatile.Read(ref error);
                    if (ex == ExceptionHelper.TERMINATED)
                    {
                        observer.OnCompleted();
                    }
                    else
                    {
                        observer.OnError(ex);
                    }
                }
                return;
            }

            var src = Volatile.Read(ref source);
            if (src != null)
            {
                src = Interlocked.Exchange(ref source, null);
                if (src != null)
                {
                    var a = cancel;
                    cancel = null;
                    a?.Invoke(this);

                    src.Subscribe(this);
                }
            }
        }

        bool Add(InnerDisposable inner)
        {
            for (; ; )
            {
                var a = Volatile.Read(ref observers);
                if (a == TERMINATED)
                {
                    return false;
                }

                var n = a.Length;
                var b = new InnerDisposable[n + 1];
                Array.Copy(a, 0, b, 0, n);
                b[n] = inner;
                if (Interlocked.CompareExchange(ref observers, b, a) == a)
                {
                    return true;
                }
            }
        }

        void Remove(InnerDisposable inner)
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
                    if (a[i] == inner)
                    {
                        j = i;
                        break;
                    }
                }

                if (j < 0)
                {
                    break;
                }

                var b = default(InnerDisposable[]);
                if (n == 1)
                {
                    b = EMPTY;
                }
                else
                {
                    b = new InnerDisposable[n - 1];
                    Array.Copy(a, 0, b, 0, j);
                    Array.Copy(a, j + 1, b, j, n - j - 1);
                }
                if (Interlocked.CompareExchange(ref observers, b, a) == a)
                {
                    break;
                }
            }
        }

        internal sealed class InnerDisposable : IDisposable
        {
            internal readonly ICompletableObserver downstream;

            CompletableCache parent;

            public InnerDisposable(ICompletableObserver downstream, CompletableCache parent)
            {
                this.downstream = downstream;
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
