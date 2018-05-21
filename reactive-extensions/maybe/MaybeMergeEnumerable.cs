using System;
using System.Collections.Generic;
using System.Threading;
using static akarnokd.reactive_extensions.ValidationHelper;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Runs and merges some or all maybe sources into one observable sequence
    /// and optionally delays all errors until all sources terminate.
    /// </summary>
    /// <typeparam name="T">The success element and result type.</typeparam>
    /// <remarks>Since 0.0.12</remarks>
    internal sealed class MaybeMergeEnumerable<T> : IObservable<T>
    {
        readonly IEnumerable<IMaybeSource<T>> sources;

        readonly bool delayErrors;

        readonly int maxConcurrency;

        public MaybeMergeEnumerable(IEnumerable<IMaybeSource<T>> sources, bool delayErrors, int maxConcurrency)
        {
            this.sources = sources;
            this.delayErrors = delayErrors;
            this.maxConcurrency = maxConcurrency;
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            var en = default(IEnumerator<IMaybeSource<T>>);

            try
            {
                en = RequireNonNullRef(sources.GetEnumerator(), "The GetEnumerator returned a null IEnumerator");
            }
            catch (Exception ex)
            {
                observer.OnError(ex);
                return DisposableHelper.EMPTY;
            }

            if (maxConcurrency == int.MaxValue)
            {
                var parent = new MergeAllCoordinator(observer, delayErrors);

                parent.Subscribe(en);

                return parent;
            }
            else
            {
                var parent = new MergeLimitedCoordinator(observer, en, delayErrors, maxConcurrency);
                parent.Drain();
                return parent;
            }
        }

        sealed class MergeLimitedCoordinator : MaybeMergeCoordinator<T>
        {
            readonly int maxConcurrency;

            IEnumerator<IMaybeSource<T>> sources;

            int active;

            bool noMoreSources;

            internal MergeLimitedCoordinator(IObserver<T> downstream, IEnumerator<IMaybeSource<T>> sources, bool delayErrors, int maxConcurrency) : base(downstream, delayErrors)
            {
                this.sources = sources;
                this.maxConcurrency = maxConcurrency;
            }

            internal override void DrainLoop()
            {
                var missed = 1;

                var sources = this.sources;
                var downstream = this.downstream;

                for (; ; )
                {
                    if (IsDisposed())
                    {
                        if (sources != null)
                        {
                            this.sources = null;
                            sources.Dispose();
                        }
                    }
                    else
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

                        if (!noMoreSources && d < maxConcurrency)
                        {
                            var src = default(IMaybeSource<T>);

                            try
                            {
                                if (sources.MoveNext())
                                {
                                    src = RequireNonNullRef(sources.Current, "The IEnumerator returned a null IMaybeSource");
                                }
                                else
                                {
                                    noMoreSources = true;
                                }
                            }
                            catch (Exception ex)
                            {
                                if (delayErrors)
                                {
                                    ExceptionHelper.AddException(ref errors, ex);
                                }
                                else
                                {
                                    Interlocked.CompareExchange(ref errors, ex, null);
                                }
                                noMoreSources = true;
                            }

                            if (noMoreSources)
                            {
                                this.sources = null;
                                sources.Dispose();
                            }
                            else
                            {
                                Interlocked.Increment(ref active);

                                SubscribeTo(src);
                                continue;
                            }
                        }

                        var q = GetQueue();

                        var v = default(T);

                        var empty = q == null || !q.TryDequeue(out v);

                        if (d == 0 && noMoreSources && empty)
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

            internal MergeAllCoordinator(IObserver<T> downstream, bool delayErrors) : base(downstream, delayErrors)
            {
            }

            internal void Subscribe(IEnumerator<IMaybeSource<T>> sources)
            {
                Interlocked.Increment(ref active);

                for (; ; )
                {
                    var src = default(IMaybeSource<T>);

                    try
                    {
                        if (sources.MoveNext())
                        {
                            src = RequireNonNullRef(sources.Current, "The IEnumerator returned a null IMaybeSource");
                        }
                        else
                        {
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        InnerError(ex);
                        break;
                    }

                    Interlocked.Increment(ref active);
                    if (!SubscribeTo(src))
                    {
                        break;
                    }
                }
                sources.Dispose();

                if (Interlocked.Decrement(ref active) == 0)
                {
                    Drain();
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
