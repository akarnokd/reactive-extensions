using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Combines the next item of each observable (a row of items)
    /// through a mapper function.
    /// </summary>
    /// <typeparam name="T">The element types of the source observables.</typeparam>
    /// <typeparam name="R">The result type.</typeparam>
    /// <remarks>Since 0.0.5</remarks>
    internal sealed class Zip<T, R> : IObservable<R>
    {
        readonly IObservable<T>[] sources;

        readonly Func<T[], R> mapper;

        readonly bool delayErrors;

        public Zip(IObservable<T>[] sources, Func<T[], R> mapper, bool delayErrors)
        {
            this.sources = sources;
            this.mapper = mapper;
            this.delayErrors = delayErrors;
        }

        public IDisposable Subscribe(IObserver<R> observer)
        {
            var srcs = sources;
            var n = srcs.Length;
            var parent = new ZipCoordinator(observer, mapper, delayErrors, n);
            parent.Subscribe(srcs);
            return parent;
        }

        sealed class ZipCoordinator : IDisposable
        {
            readonly IObserver<R> downstream;

            readonly InnerObserver[] observers;

            readonly Func<T[], R> mapper;

            readonly bool delayErrors;

            readonly bool[] hasValues;

            T[] values;

            Exception error;

            bool disposed;

            int wip;

            public ZipCoordinator(IObserver<R> downstream, Func<T[], R> mapper, bool delayErrors, int n)
            {
                this.downstream = downstream;
                this.mapper = mapper;
                this.delayErrors = delayErrors;
                var o = new InnerObserver[n];
                for (int i = 0; i < n; i++)
                {
                    o[i] = new InnerObserver(this);
                }
                this.observers = o;
                this.values = new T[n];
                this.hasValues = new bool[n];
            }

            public void Dispose()
            {
                DisposeAll();
                Drain();
            }

            public void DisposeAll()
            {
                Volatile.Write(ref disposed, true);

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

            internal void Subscribe(IObservable<T>[] sources)
            {
                var o = observers;
                var n = o.Length;

                for (int i = 0; i < n; i++)
                {
                    if (Volatile.Read(ref disposed))
                    {
                        return;
                    }
                    var inner = Volatile.Read(ref o[i]);
                    if (inner != null)
                    {
                        inner.OnSubscribe(sources[i].Subscribe(inner));
                    }
                }
            }

            internal void InnerError(InnerObserver sender, Exception ex)
            {
                if (delayErrors)
                {
                    if (ExceptionHelper.AddException(ref error, ex))
                    {
                        sender.SetDone();
                        Drain();
                    }
                }
                else
                {
                    if (Interlocked.CompareExchange(ref error, ex, null) == null)
                    {
                        Drain();
                    }
                }
            }

            internal void Drain()
            {
                if (Interlocked.Increment(ref wip) != 1)
                {
                    return;
                }

                var missed = 1;
                var observers = this.observers;
                var n = observers.Length;
                var downstream = this.downstream;
                var delayErrors = this.delayErrors;
                var hasValues = this.hasValues;

                for (; ;)
                {
                    for (; ;)
                    {
                        if (Volatile.Read(ref disposed))
                        {
                            Array.Clear(values, 0, values.Length);
                            break;
                        }

                        if (!delayErrors)
                        {
                            var ex = Volatile.Read(ref error);
                            if (ex != null)
                            {
                                downstream.OnError(ex);
                                DisposeAll();
                                continue;
                            }
                        }

                        int ready = 0;
                        bool done = false;

                        for (int i = 0; i < n; i++)
                        {
                            if (hasValues[i])
                            {
                                ready++;
                            }
                            else
                            {
                                var inner = Volatile.Read(ref observers[i]);
                                if (inner != null)
                                {
                                    var d = inner.IsDone();
                                    var empty = !inner.queue.TryDequeue(out var v);

                                    if (d && empty)
                                    {
                                        done = true;
                                        break;
                                    }

                                    if (!empty)
                                    {
                                        hasValues[i] = true;
                                        values[i] = v;
                                        ready++;
                                    }
                                }
                            }
                        }

                        if (done)
                        {
                            var ex = Volatile.Read(ref error);
                            if (ex != null)
                            {
                                downstream.OnError(ex);
                            }
                            else
                            {
                                downstream.OnCompleted();
                            }
                            DisposeAll();
                            continue;
                        }

                        if (ready == n)
                        {
                            var vals = values;
                            values = new T[n];
                            Array.Clear(hasValues, 0, hasValues.Length);

                            var result = default(R);

                            try
                            {
                                result = mapper(vals);
                            }
                            catch (Exception ex)
                            {
                                if (delayErrors)
                                {
                                    ExceptionHelper.AddException(ref error, ex);
                                    ex = ExceptionHelper.Terminate(ref error);
                                    downstream.OnError(ex);
                                    DisposeAll();
                                } else
                                {
                                    if (Interlocked.CompareExchange(ref error, ex, null) == null)
                                    {
                                        downstream.OnError(ex);
                                        DisposeAll();
                                    }
                                }
                                continue;
                            }

                            downstream.OnNext(result);
                        }
                        else
                        {
                            break;
                        }
                    }

                    missed = Interlocked.Add(ref wip, -missed);
                    if (missed == 0)
                    {
                        break;
                    }
                }
            }

            internal sealed class InnerObserver : IObserver<T>, IDisposable
            {
                readonly ZipCoordinator parent;

                internal readonly ConcurrentQueue<T> queue;

                IDisposable upstream;

                bool done;

                public InnerObserver(ZipCoordinator parent)
                {
                    this.parent = parent;
                    this.queue = new ConcurrentQueue<T>();
                }

                internal void OnSubscribe(IDisposable d)
                {
                    DisposableHelper.SetOnce(ref upstream, d);
                }

                public void Dispose()
                {
                    DisposableHelper.Dispose(ref upstream);
                }

                public void OnCompleted()
                {
                    SetDone();
                    parent.Drain();
                }

                public void OnError(Exception error)
                {
                    parent.InnerError(this, error);
                }

                public void OnNext(T value)
                {
                    queue.Enqueue(value);
                    parent.Drain();
                }

                internal bool IsDone()
                {
                    return Volatile.Read(ref done);
                }

                internal void SetDone()
                {
                    Volatile.Write(ref done, true);
                }
            }
        }
    }
}
