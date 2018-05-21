using System;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Runs and merges some or all maybe sources into one observable sequence
    /// and optionally delays all errors until all sources terminate.
    /// </summary>
    /// <typeparam name="T">The success element and result type.</typeparam>
    /// <remarks>Since 0.0.12</remarks>
    internal sealed class MaybeMerge<T> : IObservable<T>
    {
        readonly IMaybeSource<T>[] sources;

        readonly bool delayErrors;

        readonly int maxConcurrency;

        public MaybeMerge(IMaybeSource<T>[] sources, bool delayErrors, int maxConcurrency)
        {
            this.sources = sources;
            this.delayErrors = delayErrors;
            this.maxConcurrency = maxConcurrency;
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            if (maxConcurrency == int.MaxValue)
            {
                var srcs = sources;
                var n = sources.Length;

                var parent = new MergeAllCoordinator(observer, n, delayErrors);

                if (n != 0)
                {
                    parent.Subscribe(srcs, n);
                }
                else
                {
                    parent.Drain();
                }

                return parent;
            }
            else
            {
                var parent = new MergeLimitedCoordinator(observer, sources, delayErrors, maxConcurrency);
                parent.Drain();
                return parent;
            }
            
        }

        sealed class MergeLimitedCoordinator : MaybeMergeCoordinator<T>
        {
            readonly IMaybeSource<T>[] sources;

            readonly int maxConcurrency;

            int active;

            int index;

            internal MergeLimitedCoordinator(IObserver<T> downstream, IMaybeSource<T>[] sources, bool delayErrors, int maxConcurrency) : base(downstream, delayErrors)
            {
                this.sources = sources;
                this.maxConcurrency = maxConcurrency;
            }

            internal override void DrainLoop()
            {
                var missed = 1;

                var sources = this.sources;
                var n = sources.Length;
                var downstream = this.downstream;

                for (; ; )
                {
                    if (!IsDisposed())
                    {
                        if (!delayErrors)
                        {
                            var ex = Volatile.Read(ref errors);
                            if (ex != null)
                            {
                                Dispose();
                                downstream.OnError(ex);
                                continue;
                            }
                        }

                        var d = Volatile.Read(ref active);

                        var idx = index;
                        if (d < maxConcurrency && idx < n)
                        {
                            var src = sources[idx];

                            if (src == null)
                            {
                                var ex = new NullReferenceException("The IMaybeSource at index " + idx + " is null");
                                if (delayErrors)
                                {
                                    ExceptionHelper.AddException(ref errors, ex);
                                } else
                                {
                                    Interlocked.CompareExchange(ref errors, ex, null);
                                }
                                index = n;
                            }
                            else
                            {
                                index = idx + 1;
                                Interlocked.Increment(ref active);

                                SubscribeTo(src);
                            }
                            continue;
                        }

                        var q = GetQueue();

                        var v = default(T);

                        var empty = q == null || !q.TryDequeue(out v);

                        if (d == 0 && empty)
                        {
                            Volatile.Write(ref disposed, true);
                            var ex = Volatile.Read(ref errors);
                            if (ex != null)
                            {
                                downstream.OnError(ex);
                            }
                            else
                            {
                                downstream.OnCompleted();
                            }
                        }
                        else
                        if (!empty)
                        {
                            downstream.OnNext(v);
                            continue;
                        }
                    }

                    missed = Interlocked.Add(ref wip, -missed);
                    if (missed == 0)
                    {
                        break;
                    }
                }
            }

            internal override void InnerCompleted(InnerObserver sender)
            {
                Remove(sender);
                Interlocked.Decrement(ref active);
                Drain();
            }

            internal override void InnerError(InnerObserver sender, Exception ex)
            {
                Remove(sender);
                if (delayErrors)
                {
                    ExceptionHelper.AddException(ref errors, ex);
                }
                else
                {
                    Interlocked.CompareExchange(ref errors, ex, null);
                }
                Interlocked.Decrement(ref active);
                Drain();
            }

            internal override void InnerSuccess(InnerObserver sender, T item)
            {
                Remove(sender);
                if (Volatile.Read(ref wip) == 0 && Interlocked.CompareExchange(ref wip, 1, 0) == 0)
                {
                    downstream.OnNext(item);
                    Interlocked.Decrement(ref active);
                }
                else
                {
                    var q = GetOrCreateQueue();
                    q.Enqueue(item);

                    Interlocked.Decrement(ref active);
                    if (Interlocked.Increment(ref wip) != 1)
                    {
                        return;
                    }
                }

                DrainLoop();
            }
        }

        sealed class MergeAllCoordinator : MaybeMergeCoordinator<T>
        {
            int active;

            internal MergeAllCoordinator(IObserver<T> downstream, int n, bool delayErrors) : base(downstream, delayErrors)
            {
                Volatile.Write(ref active, n);
            }

            internal void Subscribe(IMaybeSource<T>[] sources, int n)
            {
                for (int i = 0; i < n; i++)
                {
                    IMaybeSource<T> src = sources[i];
                    if (src == null)
                    {
                        var ex = new NullReferenceException("The IMaybeSource at index " + i + " is null");
                        InnerError(ex);
                        Interlocked.Add(ref active, -(n - i));
                        Drain();
                        break;
                    }
                    if (!SubscribeTo(src))
                    {
                        break;
                    }
                }

            }

            internal override void DrainLoop()
            {
                var missed = 1;
                var downstream = this.downstream;

                for (; ; )
                {
                    if (!IsDisposed())
                    {
                        if (!delayErrors)
                        {
                            var ex = Volatile.Read(ref errors);
                            if (ex != null)
                            {
                                Dispose();
                                downstream.OnError(ex);
                                continue;
                            }
                        }

                        var d = Volatile.Read(ref active) == 0;

                        var q = GetQueue();

                        var v = default(T);
                        var empty = q == null || !q.TryDequeue(out v);

                        if (d && empty)
                        {
                            Volatile.Write(ref disposed, true);
                            var ex = Volatile.Read(ref errors);
                            if (ex != null)
                            {
                                downstream.OnError(ex);
                            }
                            else
                            {
                                downstream.OnCompleted();
                            }
                        }
                        else
                        if (!empty)
                        {
                            downstream.OnNext(v);
                            continue;
                        }
                    }

                    missed = Interlocked.Add(ref wip, -missed);
                    if (missed == 0)
                    {
                        break;
                    }
                }
            }

            internal override void InnerCompleted(InnerObserver sender)
            {
                Remove(sender);
                Interlocked.Decrement(ref active);
                Drain();
            }

            internal void InnerError(Exception ex)
            {
                if (delayErrors)
                {
                    ExceptionHelper.AddException(ref errors, ex);
                }
                else
                {
                    Interlocked.CompareExchange(ref errors, ex, null);
                }
            }

            internal override void InnerError(InnerObserver sender, Exception ex)
            {
                Remove(sender);
                InnerError(ex);
                Interlocked.Decrement(ref active);
                Drain();
            }

            internal override void InnerSuccess(InnerObserver sender, T item)
            {
                Remove(sender);
                if (Volatile.Read(ref wip) == 0 && Interlocked.CompareExchange(ref wip, 1, 0) == 0)
                {
                    downstream.OnNext(item);
                    if (Interlocked.Decrement(ref active) != 0)
                    {
                        if (Interlocked.Decrement(ref wip) == 0)
                        {
                            return;
                        }
                    }
                }
                else
                {
                    var q = GetOrCreateQueue();
                    q.Enqueue(item);

                    Interlocked.Decrement(ref active);
                    if (Interlocked.Increment(ref wip) != 1)
                    {
                        return;
                    }
                }

                DrainLoop();
            }
        }
    }
}
