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
    /// <remarks>Since 0.0.21</remarks>
    internal sealed class ObservableSourceZip<T, R> : IObservableSource<R>
    {
        readonly IObservableSource<T>[] sources;

        readonly Func<T[], R> mapper;

        readonly bool delayErrors;

        readonly int capacityHint;

        public ObservableSourceZip(IObservableSource<T>[] sources, Func<T[], R> mapper, bool delayErrors, int capacityHint)
        {
            this.sources = sources;
            this.mapper = mapper;
            this.delayErrors = delayErrors;
            this.capacityHint = capacityHint;
        }

        public void Subscribe(ISignalObserver<R> observer)
        {
            var srcs = sources;
            var n = srcs.Length;
            var parent = new ZipCoordinator(observer, mapper, delayErrors, n, capacityHint);
            observer.OnSubscribe(parent);
            parent.Subscribe(srcs);
        }

        sealed class ZipCoordinator : IDisposable
        {
            readonly ISignalObserver<R> downstream;

            readonly InnerObserver[] observers;

            readonly Func<T[], R> mapper;

            readonly bool delayErrors;

            readonly bool[] hasValues;

            readonly int capacityHint;

            T[] values;

            Exception error;

            bool disposed;

            int wip;

            public ZipCoordinator(ISignalObserver<R> downstream, Func<T[], R> mapper, bool delayErrors, int n, int capacityHint)
            {
                this.downstream = downstream;
                this.mapper = mapper;
                this.delayErrors = delayErrors;
                this.capacityHint = capacityHint;
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

            internal void Subscribe(IObservableSource<T>[] sources)
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
                        sources[i].Subscribe(inner);
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
                                    var q = Volatile.Read(ref inner.queue);
                                    var v = default(T);
                                    var success = false;
                                    
                                    try
                                    {
                                        if (q != null)
                                        {
                                            v = q.TryPoll(out success);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        if (delayErrors)
                                        {
                                            inner.Dispose();
                                            ExceptionHelper.AddException(ref error, ex);
                                            inner.SetDone();
                                            d = true;
                                        }
                                        else
                                        {
                                            Interlocked.CompareExchange(ref error, ex, null);
                                            ex = Volatile.Read(ref error);
                                            downstream.OnError(ex);
                                            DisposeAll();
                                            continue;
                                        }
                                    }

                                    if (d && !success)
                                    {
                                        done = true;
                                        break;
                                    }

                                    if (success)
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

            internal sealed class InnerObserver : ISignalObserver<T>, IDisposable
            {
                readonly ZipCoordinator parent;

                internal ISimpleQueue<T> queue;

                IDisposable upstream;

                bool done;

                int fusionMode;

                public InnerObserver(ZipCoordinator parent)
                {
                    this.parent = parent;
                }

                public void OnSubscribe(IDisposable d)
                {
                    if (DisposableHelper.SetOnce(ref upstream, d))
                    {
                        if (d is IFuseableDisposable<T> fd)
                        {
                            var m = fd.RequestFusion(FusionSupport.AnyBoundary);

                            if (m == FusionSupport.Sync)
                            {
                                fusionMode = m;
                                Volatile.Write(ref queue, fd);
                                SetDone();
                                parent.Drain();
                                return;
                            }
                            if (m == FusionSupport.Async)
                            {
                                fusionMode = m;
                                Volatile.Write(ref queue, fd);
                                return;
                            }
                        }

                        queue = new SpscLinkedArrayQueue<T>(parent.capacityHint);
                    }
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
                    if (fusionMode == FusionSupport.None)
                    {
                        queue.TryOffer(value);
                    }
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
