using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Combines the latest items of each source observable through
    /// a mapper function.
    /// </summary>
    /// <typeparam name="T">The element type of the observables.</typeparam>
    /// <typeparam name="R">The result type.</typeparam>
    /// <remarks>Since 0.0.21</remarks>
    internal sealed class ObservableSourceCombineLatest<T, R> : IObservableSource<R>
    {
        readonly IObservableSource<T>[] sources;

        readonly Func<T[], R> mapper;

        readonly bool delayErrors;

        public ObservableSourceCombineLatest(IObservableSource<T>[] sources, Func<T[], R> mapper, bool delayErrors)
        {
            this.sources = sources;
            this.mapper = mapper;
            this.delayErrors = delayErrors;
        }

        public void Subscribe(ISignalObserver<R> observer)
        {
            var srcs = sources;
            var n = srcs.Length;

            var parent = new CombineCoordinator(observer, mapper, delayErrors, n);
            observer.OnSubscribe(parent);

            parent.Subscribe(srcs);
        }

        sealed class CombineCoordinator : IDisposable
        {
            readonly ISignalObserver<R> downstream;

            readonly Func<T[], R> mapper;

            readonly bool delayErrors;

            readonly InnerObserver[] observers;

            readonly ConcurrentQueue<Signal> queue;

            readonly T[] values;

            readonly bool[] hasLatest;

            int available;

            int done;
            Exception error;

            bool disposed;

            int wip;

            internal CombineCoordinator(ISignalObserver<R> downstream, Func<T[], R> mapper, bool delayErrors, int n)
            {
                this.downstream = downstream;
                this.mapper = mapper;
                this.delayErrors = delayErrors;
                this.hasLatest = new bool[n];
                var o = new InnerObserver[n];
                for (int i = 0; i < n; i++)
                {
                    o[i] = new InnerObserver(this, i);
                }
                this.observers = o;
                this.values = new T[n];
                this.queue = new ConcurrentQueue<Signal>();
            }

            public void Dispose()
            {
                Volatile.Write(ref disposed, true);
                DisposeAll();
                Drain();
            }

            public void DisposeAll()
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

            internal void Subscribe(IObservableSource<T>[] observables)
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
                        observables[i].Subscribe(inner);
                    }
                }
            }

            void Drain()
            {
                if (Interlocked.Increment(ref wip) != 1)
                {
                    return;
                }

                var missed = 1;
                var downstream = this.downstream;
                var queue = this.queue;
                var observers = this.observers;
                var n = observers.Length;
                var values = this.values;
                var hasLatest = this.hasLatest;

                for (; ; )
                {
                    for (; ;)
                    {
                        if (Volatile.Read(ref disposed))
                        {
                            while (queue.TryDequeue(out var _)) ;
                            for (int i = 0; i < n; i++)
                            {
                                values[i] = default(T);
                            }
                            break;
                        }

                        if (!delayErrors)
                        {
                            var ex = Volatile.Read(ref error);
                            if (ex != null)
                            {
                                downstream.OnError(ex);
                                Volatile.Write(ref disposed, true);
                                DisposeAll();
                                continue;
                            }
                        }

                        var d = Volatile.Read(ref done) == n;
                        var empty = !queue.TryDequeue(out var v);

                        if (d && empty)
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
                            Volatile.Write(ref disposed, true);
                            DisposeAll();
                            continue;
                        }

                        if (empty)
                        {
                            break;
                        }

                        var idx = v.index;
                        if (!hasLatest[idx])
                        {
                            hasLatest[idx] = true;
                            available++;
                        }
                        values[idx] = v.value;

                        if (available == n)
                        {
                            var result = default(R);

                            try
                            {
                                var a = new T[n];
                                Array.Copy(values, a, n);
                                result = mapper(a);
                            }
                            catch (Exception ex)
                            {
                                if (delayErrors)
                                {
                                    ExceptionHelper.AddException(ref error, ex);
                                    ex = ExceptionHelper.Terminate(ref error);
                                    downstream.OnError(ex);
                                    Volatile.Write(ref disposed, true);
                                    DisposeAll();
                                }
                                else
                                {
                                    if (Interlocked.CompareExchange(ref error, ex, null) == null)
                                    {
                                        downstream.OnError(ex);
                                        Volatile.Write(ref disposed, true);
                                        DisposeAll();
                                    }
                                }
                                continue;
                            }

                            downstream.OnNext(result);
                        }
                    }

                    missed = Interlocked.Add(ref wip, -missed);
                    if (missed == 0)
                    {
                        break;
                    }
                }
            }

            void InnerNext(int index, T item)
            {
                queue.Enqueue(new Signal { index = index, value = item });
                Drain();
            }

            void InnerError(Exception ex)
            {
                if (delayErrors)
                {
                    if (ExceptionHelper.AddException(ref error, ex))
                    {
                        Interlocked.Increment(ref done);
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

            void InnerComplete()
            {
                Interlocked.Increment(ref done);
                Drain();
            }

            internal struct Signal
            {
                internal int index;
                internal T value;
            }

            internal sealed class InnerObserver : ISignalObserver<T>, IDisposable
            {
                readonly CombineCoordinator parent;

                readonly int index;

                IDisposable upstream;

                public InnerObserver(CombineCoordinator parent, int index)
                {
                    this.parent = parent;
                    this.index = index;
                }

                public void OnSubscribe(IDisposable d)
                {
                    DisposableHelper.SetOnce(ref upstream, d);
                }

                public void Dispose()
                {
                    DisposableHelper.Dispose(ref upstream);
                }

                public void OnCompleted()
                {
                    parent.InnerComplete();
                }

                public void OnError(Exception error)
                {
                    parent.InnerError(error);
                }

                public void OnNext(T value)
                {
                    parent.InnerNext(index, value);
                }
            }
        }
    }
}
