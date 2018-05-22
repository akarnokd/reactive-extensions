using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Relays the signals of the first single source to respond
    /// and disposes the other sources.
    /// </summary>
    /// <typeparam name="T">The success value type.</typeparam>
    /// <remarks>Since 0.0.12</remarks>
    internal sealed class SingleAmb<T> : ISingleSource<T>
    {
        readonly ISingleSource<T>[] sources;

        public SingleAmb(ISingleSource<T>[] sources)
        {
            this.sources = sources;
        }

        public void Subscribe(ISingleObserver<T> observer)
        {
            var srcs = sources;
            var n = srcs.Length;

            SingleAmbCoordinator<T>.Run(observer, n, srcs);
        }
    }

    /// <summary>
    /// Coordinates the signals of multiple observers.
    /// </summary>
    /// <typeparam name="T">The success value type.</typeparam>
    /// <remarks>Since 0.0.12</remarks>
    internal sealed class SingleAmbCoordinator<T> : IDisposable
    {
        readonly ISingleObserver<T> downstream;

        readonly InnerObserver[] observers;

        int winner;

        internal static void Run(ISingleObserver<T> observer, int n, ISingleSource<T>[] sources)
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

            var parent = new SingleAmbCoordinator<T>(observer, n);
            observer.OnSubscribe(parent);

            parent.Subscribe(sources, n);
        }

        public SingleAmbCoordinator(ISingleObserver<T> downstream, int n)
        {
            this.downstream = downstream;
            var o = new InnerObserver[n];
            for (int i = 0; i < n; i++)
            {
                o[i] = new InnerObserver(this, i);
            }
            this.observers = o;
            Volatile.Write(ref winner, -1);
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

        internal void Subscribe(ISingleSource<T>[] sources, int n)
        {
            var o = observers;
            for (int i = 0; i < n; i++)
            {
                var inner = Volatile.Read(ref o[i]);
                if (inner == null)
                {
                    break;
                }

                var src = sources[i];

                if (src == null)
                {
                    InnerError(i, new NullReferenceException("The ISingleSource at index " + i + " is null"));
                    break;
                }

                sources[i].Subscribe(inner);
            }
        }

        void InnerSuccess(int index, T item)
        {
            Volatile.Write(ref observers[index], null);

            if (Volatile.Read(ref winner) == -1 && Interlocked.CompareExchange(ref winner, index, -1) == -1)
            {
                Dispose();
                downstream.OnSuccess(item);
            }
        }

        void InnerError(int index, Exception ex)
        {
            Volatile.Write(ref observers[index], null);

            if (Volatile.Read(ref winner) == -1 && Interlocked.CompareExchange(ref winner, index, -1) == -1)
            {
                Dispose();
                downstream.OnError(ex);
            }
        }

        sealed class InnerObserver : ISingleObserver<T>, IDisposable
        {
            readonly SingleAmbCoordinator<T> parent;

            readonly int index;

            IDisposable upstream;

            public InnerObserver(SingleAmbCoordinator<T> parent, int index)
            {
                this.parent = parent;
                this.index = index;
            }

            public void Dispose()
            {
                DisposableHelper.Dispose(ref upstream);
            }

            public void OnError(Exception error)
            {
                parent.InnerError(index, error);
            }

            public void OnSubscribe(IDisposable d)
            {
                DisposableHelper.SetOnce(ref upstream, d);
            }

            public void OnSuccess(T item)
            {
                parent.InnerSuccess(index, item);
            }
        }
    }

    /// <summary>
    /// Relays the signals of the first single source to respond
    /// and disposes the other sources.
    /// </summary>
    /// <typeparam name="T">The success value type.</typeparam>
    /// <remarks>Since 0.0.12</remarks>
    internal sealed class SingleAmbEnumerable<T> : ISingleSource<T>
    {
        readonly IEnumerable<ISingleSource<T>> sources;

        public SingleAmbEnumerable(IEnumerable<ISingleSource<T>> sources)
        {
            this.sources = sources;
        }

        public void Subscribe(ISingleObserver<T> observer)
        {
            var n = 0;
            var srcs = sources;

            var a = new ISingleSource<T>[8];

            try
            {
                foreach (var m in srcs)
                {
                    if (n == a.Length)
                    {
                        var b = new ISingleSource<T>[n + (n >> 2)];
                        Array.Copy(a, 0, b, 0, n);
                        a = b;
                    }
                    a[n++] = m;
                }
            }
            catch (Exception ex)
            {
                DisposableHelper.Error(observer, ex);
                return;
            }

            SingleAmbCoordinator<T>.Run(observer, n, a);
        }
    }
}
