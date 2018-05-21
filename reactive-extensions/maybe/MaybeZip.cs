using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Waits for all maybe sources to produce a success item and
    /// calls the mapper function to generate
    /// the output success value to be signaled to the downstream.
    /// </summary>
    /// <typeparam name="T">The success value type of the sources.</typeparam>
    /// <typeparam name="R">The output success value type.</typeparam>
    /// <remarks>Since 0.0.12</remarks>
    internal sealed class MaybeZip<T, R> : IMaybeSource<R>
    {
        readonly IMaybeSource<T>[] sources;

        readonly Func<T[], R> mapper;

        readonly bool delayErrors;

        public MaybeZip(IMaybeSource<T>[] sources, Func<T[], R> mapper, bool delayErrors)
        {
            this.sources = sources;
            this.mapper = mapper;
            this.delayErrors = delayErrors;
        }

        public void Subscribe(IMaybeObserver<R> observer)
        {
            var srcs = sources;
            var n = srcs.Length;

            MaybeZipCoordinator<T, R>.Run(srcs, n, observer, mapper, delayErrors);
        }
    }

    /// <summary>
    /// Coordinates multiple observers and their signals.
    /// </summary>
    /// <typeparam name="T">The element type of the sources.</typeparam>
    /// <typeparam name="R">The result type.</typeparam>
    /// <remarks>Since 0.0.12</remarks>
    internal sealed class MaybeZipCoordinator<T, R> : IDisposable
    {
        internal readonly IMaybeObserver<R> downstream;

        readonly InnerObserver[] observers;

        readonly Func<T[], R> mapper;

        readonly bool delayErrors;

        T[] values;

        bool disposed;

        int ready;

        Exception error;

        bool hasEmpty;

        internal static void Run(IMaybeSource<T>[] srcs, int n, IMaybeObserver<R> observer, Func<T[], R> mapper, bool delayErrors)
        {
            if (n == 0)
            {
                DisposableHelper.Complete(observer);
                return;
            }

            if (n == 1)
            {
                new MaybeMap<T, R>(srcs[0],
                    v => mapper(new T[] { v })
                ).Subscribe(observer);
                return;
            }

            var parent = new MaybeZipCoordinator<T, R>(observer, n, mapper, delayErrors);
            observer.OnSubscribe(parent);

            parent.Subscribe(srcs, n);

        }

        internal MaybeZipCoordinator(IMaybeObserver<R> downstream, int n, Func<T[], R> mapper, bool delayErrors)
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

        public void Subscribe(IMaybeSource<T>[] sources, int n)
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
                    InnerError(i, new NullReferenceException("The IMaybeSource at index " + i + " is null"));
                }
                else
                {
                    sources[i].Subscribe(o[i]);
                }
            }
        }

        void InnerCompleted(int index)
        {
            if (delayErrors)
            {
                hasEmpty = true;
                if (Interlocked.Decrement(ref ready) == 0)
                {
                    Volatile.Write(ref values, null);

                    var ex = error;
                    if (ex != null)
                    {
                        downstream.OnError(ex);
                    }
                    else
                    {
                        downstream.OnCompleted();
                    }
                }
            }
            else
            {
                DisposeAll();
                if (Interlocked.Exchange(ref ready, 0) > 0)
                {
                    Volatile.Write(ref values, null);

                    downstream.OnCompleted();
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

                        if (hasEmpty)
                        {
                            downstream.OnCompleted();
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

        sealed class InnerObserver : IMaybeObserver<T>, IDisposable
        {
            readonly MaybeZipCoordinator<T, R> parent;

            readonly int index;

            IDisposable upstream;

            public InnerObserver(MaybeZipCoordinator<T, R> parent, int index)
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

            public void OnSuccess(T item)
            {
                DisposableHelper.WeakDispose(ref upstream);
                parent.InnerSuccess(index, item);
            }
        }
    }

    /// <summary>
    /// Waits for all maybe sources to produce a success item and
    /// calls the mapper function to generate
    /// the output success value to be signaled to the downstream.
    /// </summary>
    /// <typeparam name="T">The success value type of the sources.</typeparam>
    /// <typeparam name="R">The output success value type.</typeparam>
    /// <remarks>Since 0.0.12</remarks>
    internal sealed class MaybeZipEnumerable<T, R> : IMaybeSource<R>
    {
        readonly IEnumerable<IMaybeSource<T>> sources;

        readonly Func<T[], R> mapper;

        readonly bool delayErrors;

        public MaybeZipEnumerable(IEnumerable<IMaybeSource<T>> sources, Func<T[], R> mapper, bool delayErrors)
        {
            this.sources = sources;
            this.mapper = mapper;
            this.delayErrors = delayErrors;
        }

        public void Subscribe(IMaybeObserver<R> observer)
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

            MaybeZipCoordinator<T, R>.Run(a, n, observer, mapper, delayErrors);
        }
    }
}
