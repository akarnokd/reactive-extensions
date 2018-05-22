using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Waits for all single sources to produce a success item and
    /// calls the mapper function to generate
    /// the output success value to be signaled to the downstream.
    /// </summary>
    /// <typeparam name="T">The success value type of the sources.</typeparam>
    /// <typeparam name="R">The output success value type.</typeparam>
    /// <remarks>Since 0.0.12</remarks>
    internal sealed class SingleZip<T, R> : ISingleSource<R>
    {
        readonly ISingleSource<T>[] sources;

        readonly Func<T[], R> mapper;

        readonly bool delayErrors;

        public SingleZip(ISingleSource<T>[] sources, Func<T[], R> mapper, bool delayErrors)
        {
            this.sources = sources;
            this.mapper = mapper;
            this.delayErrors = delayErrors;
        }

        public void Subscribe(ISingleObserver<R> observer)
        {
            var srcs = sources;
            var n = srcs.Length;

            SingleZipCoordinator<T, R>.Run(srcs, n, observer, mapper, delayErrors);
        }
    }

    /// <summary>
    /// Coordinates multiple observers and their signals.
    /// </summary>
    /// <typeparam name="T">The element type of the sources.</typeparam>
    /// <typeparam name="R">The result type.</typeparam>
    /// <remarks>Since 0.0.12</remarks>
    internal sealed class SingleZipCoordinator<T, R> : IDisposable
    {
        internal readonly ISingleObserver<R> downstream;

        readonly InnerObserver[] observers;

        readonly Func<T[], R> mapper;

        readonly bool delayErrors;

        T[] values;

        bool disposed;

        int ready;

        Exception error;

        internal static void Run(ISingleSource<T>[] srcs, int n, ISingleObserver<R> observer, Func<T[], R> mapper, bool delayErrors)
        {
            if (n == 0)
            {
                DisposableHelper.Complete(observer);
                return;
            }

            if (n == 1)
            {
                new SingleMap<T, R>(srcs[0],
                    v => mapper(new T[] { v })
                ).Subscribe(observer);
                return;
            }

            var parent = new SingleZipCoordinator<T, R>(observer, n, mapper, delayErrors);
            observer.OnSubscribe(parent);

            parent.Subscribe(srcs, n);

        }

        internal SingleZipCoordinator(ISingleObserver<R> downstream, int n, Func<T[], R> mapper, bool delayErrors)
        {
            this.downstream = downstream;
            var o = new InnerObserver[n];

            for (int i = 0; i < n; i++)
            {
                o[i] = new InnerObserver(this, i);
            }

            this.observers = o;
            this.mapper = mapper;
            this.values = new T[n];
            this.delayErrors = delayErrors;
            Volatile.Write(ref ready, n);
        }

        public void Dispose()
        {
            Volatile.Write(ref disposed, true);
            DisposeAll();
        }

        public void DisposeAll()
        {
            foreach (var inner in observers)
            {
                inner.Dispose();
            }
            Volatile.Write(ref values, null);
        }

        public void Subscribe(ISingleSource<T>[] sources, int n)
        {
            var o = observers;
            for (int i = 0; i < n; i++)
            {
                if (Volatile.Read(ref disposed))
                {
                    break;
                }
                var src = sources[i];
                if (src == null)
                {
                    InnerError(i, new NullReferenceException("The ISingleSource at index " + i + " is null"));
                }
                else
                {
                    sources[i].Subscribe(o[i]);
                }
            }
        }

        void InnerError(int index, Exception ex)
        {
            if (delayErrors)
            {
                ExceptionHelper.AddException(ref error, ex);
                if (Interlocked.Decrement(ref ready) == 0)
                {
                    Volatile.Write(ref values, null);
                    downstream.OnError(error);
                }
            }
            else
            {
                DisposeAll();
                if (Interlocked.Exchange(ref ready, 0) > 0)
                {
                    Volatile.Write(ref values, null);
                    downstream.OnError(ex);
                }
            }
        }

        void InnerSuccess(int index, T item)
        {
            var v = Volatile.Read(ref values);
            if (v != null)
            {
                v[index] = item;

                if (Interlocked.Decrement(ref ready) == 0)
                {
                    Volatile.Write(ref values, null);

                    if (delayErrors)
                    {
                        var ex = error;
                        if (ex != null)
                        {
                            downstream.OnError(ex);
                            return;
                        }
                    }

                    var w = default(R);

                    try
                    {
                        w = mapper(v);
                    }
                    catch (Exception exc)
                    {
                        downstream.OnError(exc);
                        return;
                    }

                    downstream.OnSuccess(w);
                }
            }
        }

        sealed class InnerObserver : ISingleObserver<T>, IDisposable
        {
            readonly SingleZipCoordinator<T, R> parent;

            readonly int index;

            IDisposable upstream;

            public InnerObserver(SingleZipCoordinator<T, R> parent, int index)
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
                DisposableHelper.WeakDispose(ref upstream);
                parent.InnerError(index, error);
            }

            public void OnSubscribe(IDisposable d)
            {
                DisposableHelper.SetOnce(ref upstream, d);
            }

            public void OnSuccess(T item)
            {
                DisposableHelper.WeakDispose(ref upstream);
                parent.InnerSuccess(index, item);
            }
        }
    }

    /// <summary>
    /// Waits for all single sources to produce a success item and
    /// calls the mapper function to generate
    /// the output success value to be signaled to the downstream.
    /// </summary>
    /// <typeparam name="T">The success value type of the sources.</typeparam>
    /// <typeparam name="R">The output success value type.</typeparam>
    /// <remarks>Since 0.0.12</remarks>
    internal sealed class SingleZipEnumerable<T, R> : ISingleSource<R>
    {
        readonly IEnumerable<ISingleSource<T>> sources;

        readonly Func<T[], R> mapper;

        readonly bool delayErrors;

        public SingleZipEnumerable(IEnumerable<ISingleSource<T>> sources, Func<T[], R> mapper, bool delayErrors)
        {
            this.sources = sources;
            this.mapper = mapper;
            this.delayErrors = delayErrors;
        }

        public void Subscribe(ISingleObserver<R> observer)
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

            SingleZipCoordinator<T, R>.Run(a, n, observer, mapper, delayErrors);
        }
    }
}
