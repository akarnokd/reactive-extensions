using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Relays the signals of the first maybe source to respond
    /// and disposes the other sources.
    /// </summary>
    /// <typeparam name="T">The success value type.</typeparam>
    /// <remarks>Since 0.0.12</remarks>
    internal sealed class MaybeAmb<T> : IMaybeSource<T>
    {
        readonly IMaybeSource<T>[] sources;

        public MaybeAmb(IMaybeSource<T>[] sources)
        {
            this.sources = sources;
        }

        public void Subscribe(IMaybeObserver<T> observer)
        {
            var srcs = sources;
            var n = srcs.Length;

            MaybeAmbCoordinator<T>.Run(observer, n, srcs);
        }
    }

    /// <summary>
    /// Coordinates the signals of multiple observers.
    /// </summary>
    /// <typeparam name="T">The success value type.</typeparam>
    /// <remarks>Since 0.0.12</remarks>
    internal sealed class MaybeAmbCoordinator<T> : IDisposable
    {
        readonly IMaybeObserver<T> downstream;

        readonly InnerObserver[] observers;

        int winner;

        internal static void Run(IMaybeObserver<T> observer, int n, IMaybeSource<T>[] sources)
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

            var parent = new MaybeAmbCoordinator<T>(observer, n);
            observer.OnSubscribe(parent);

            parent.Subscribe(sources, n);
        }

        public MaybeAmbCoordinator(IMaybeObserver<T> downstream, int n)
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

        internal void Subscribe(IMaybeSource<T>[] sources, int n)
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
                    InnerError(i, new NullReferenceException("The IMaybeSource at index " + i + " is null"));
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

        void InnerComplete(int index)
        {
            Volatile.Write(ref observers[index], null);

            if (Volatile.Read(ref winner) == -1 && Interlocked.CompareExchange(ref winner, index, -1) == -1)
            {
                Dispose();
                downstream.OnCompleted();
            }
        }

        sealed class InnerObserver : IMaybeObserver<T>, IDisposable
        {
            readonly MaybeAmbCoordinator<T> parent;

            readonly int index;

            IDisposable upstream;

            public InnerObserver(MaybeAmbCoordinator<T> parent, int index)
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
                parent.InnerComplete(index);
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
    /// Relays the signals of the first maybe source to respond
    /// and disposes the other sources.
    /// </summary>
    /// <typeparam name="T">The success value type.</typeparam>
    /// <remarks>Since 0.0.12</remarks>
    internal sealed class MaybeAmbEnumerable<T> : IMaybeSource<T>
    {
        readonly IEnumerable<IMaybeSource<T>> sources;

        public MaybeAmbEnumerable(IEnumerable<IMaybeSource<T>> sources)
        {
            this.sources = sources;
        }

        public void Subscribe(IMaybeObserver<T> observer)
        {
            var n = 0;
            var srcs = sources;

            var a = new IMaybeSource<T>[8];

            try
            {
                foreach (var m in srcs)
                {
                    if (n == a.Length)
                    {
                        var b = new IMaybeSource<T>[n + (n >> 2)];
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

            MaybeAmbCoordinator<T>.Run(observer, n, a);
        }
    }
}
