using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Relays the terminal event of the fastest responding
    /// completable source and disposes the others.
    /// </summary>
    /// <remarks>Since 0.0.7</remarks>
    internal sealed class CompletableAmb : ICompletableSource
    {
        readonly ICompletableSource[] sources;

        public CompletableAmb(ICompletableSource[] sources)
        {
            this.sources = sources;
        }

        public void Subscribe(ICompletableObserver observer)
        {
            var o = sources;
            var n = o.Length;

            Subscribe(observer, o, n);
        }

        internal static void Subscribe(ICompletableObserver observer, ICompletableSource[] sources, int n)
        {
            if (n == 0)
            {
                DisposableHelper.Complete(observer);
                return;
            }
            if (n == 1)
            {
                sources[0].Subscribe(observer);
                return;
            }

            var parent = new AmbDisposable(observer, n);
            observer.OnSubscribe(parent);

            var co = parent.observers;

            for (int i = 0; i < n; i++)
            {
                if (!parent.IsDisposed())
                {
                    sources[i].Subscribe(co[i]);
                }
            }
        }

        internal sealed class AmbDisposable : IDisposable
        {
            readonly ICompletableObserver downstream;

            internal readonly AmbObserver[] observers;

            int index;

            internal AmbDisposable(ICompletableObserver downstream, int n)
            {
                this.downstream = downstream;
                var o = new AmbObserver[n];
                for (int i = 0; i < n; i++)
                {
                    o[i] = new AmbObserver(this, i);
                }
                observers = o;
                Volatile.Write(ref index, -1);
            }

            internal bool IsDisposed()
            {
                return Volatile.Read(ref observers[0]) == null;
            }

            public void Dispose()
            {
                var o = observers;
                var n = o.Length;

                for (int i = 0; i < n; i++)
                {
                    if (Volatile.Read(ref o[i]) != null)
                    {
                        Interlocked.Exchange(ref o[i], null)?.Dispose();
                    }
                }
            }

            void InnerCompleted(int index)
            {
                if (Interlocked.CompareExchange(ref this.index, index, -1) == -1)
                {
                    Volatile.Write(ref observers[index], null);
                    Dispose();
                    downstream.OnCompleted();
                }
            }

            void InnerError(int index, Exception error)
            {
                if (Interlocked.CompareExchange(ref this.index, index, -1) == -1)
                {
                    Volatile.Write(ref observers[index], null);
                    Dispose();
                    downstream.OnError(error);
                }
            }

            internal sealed class AmbObserver : ICompletableObserver, IDisposable
            {
                readonly AmbDisposable parent;

                readonly int index;

                IDisposable upstream;

                public AmbObserver(AmbDisposable parent, int index)
                {
                    this.parent = parent;
                    this.index = index;
                }

                public void Dispose()
                {
                    DisposableHelper.Dispose(ref upstream);
                }

                public void OnCompleted()
                {
                    DisposableHelper.WeakDispose(ref upstream);
                    parent.InnerCompleted(index);
                }

                public void OnError(Exception error)
                {
                    DisposableHelper.WeakDispose(ref upstream);
                    parent.InnerError(index, error);
                }

                public void OnSubscribe(IDisposable d)
                {
                    DisposableHelper.SetOnce(ref upstream, d);
                }
            }
        }
    }

    /// <summary>
    /// Relays the terminal event of the fastest responding
    /// completable source and disposes the others.
    /// </summary>
    /// <remarks>Since 0.0.7</remarks>
    internal sealed class CompletableAmbEnumerable : ICompletableSource
    {
        readonly IEnumerable<ICompletableSource> sources;

        public CompletableAmbEnumerable(IEnumerable<ICompletableSource> sources)
        {
            this.sources = sources;
        }

        public void Subscribe(ICompletableObserver observer)
        {
            var srcs = sources;

            var n = 0;
            var o = new ICompletableSource[8];

            try
            {
                foreach (var s in srcs)
                {
                    if (n == o.Length)
                    {
                        var b = new ICompletableSource[n + (n >> 2)];
                        Array.Copy(o, 0, b, 0, n);
                        o = b;
                    }
                    o[n++] = s;
                }
            }
            catch (Exception ex)
            {
                DisposableHelper.Error(observer, ex);
                return;
            }

            CompletableAmb.Subscribe(observer, o, n);
        }
    }
}
