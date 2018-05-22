using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using static akarnokd.reactive_extensions.ValidationHelper;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Maps the values of an observable sequence into maybe sources,
    /// runs and merges some or all maybe sources
    /// into one observable sequence and optionally delays all errors until all sources terminate.
    /// </summary>
    /// <typeparam name="T">The value type of the source observable sequence.</typeparam>
    /// <typeparam name="R">The success value type of the inner maybe sources.</typeparam>
    /// <remarks>Since 0.0.13</remarks>
    internal sealed class MaybeFlatMapMany<T, R> : IObservable<R>
    {
        readonly IObservable<T> sources;

        readonly Func<T, IMaybeSource<R>> mapper;

        readonly bool delayErrors;

        readonly int maxConcurrency;

        public MaybeFlatMapMany(IObservable<T> sources, Func<T, IMaybeSource<R>> mapper, bool delayErrors, int maxConcurrency)
        {
            this.sources = sources;
            this.mapper = mapper;
            this.delayErrors = delayErrors;
            this.maxConcurrency = maxConcurrency;
        }

        public IDisposable Subscribe(IObserver<R> observer)
        {
            if (maxConcurrency == int.MaxValue)
            {
                var parent = new FlatMapAllCoordinator(observer, mapper, delayErrors);

                parent.OnSubscribe(sources.Subscribe(parent));

                return parent;
            }
            else
            {
                var parent = new FlatMapLimitedCoordinator(observer, mapper, delayErrors, maxConcurrency);

                parent.OnSubscribe(sources.Subscribe(parent));

                return parent;
            }
        }

        sealed class FlatMapLimitedCoordinator : MaybeMergeCoordinator<R>, IObserver<T>
        {
            readonly Func<T, IMaybeSource<R>> mapper;

            readonly ConcurrentQueue<T> sources;

            readonly int maxConcurrency;

            int active;

            bool done;

            IDisposable upstream;

            bool noMoreSources;

            internal FlatMapLimitedCoordinator(IObserver<R> downstream, Func<T, IMaybeSource<R>> mapper, 
                bool delayErrors, int maxConcurrency) : base(downstream, delayErrors)
            {
                this.mapper = mapper;
                this.maxConcurrency = maxConcurrency;
                this.sources = new ConcurrentQueue<T>();
            }

            public void OnSubscribe(IDisposable d)
            {
                DisposableHelper.SetOnce(ref upstream, d);
            }

            public void OnNext(T item)
            {
                sources.Enqueue(item);
                Drain();
            }

            public void OnError(Exception ex)
            {
                if (delayErrors)
                {
                    ExceptionHelper.AddException(ref errors, ex);
                }
                else
                {
                    Interlocked.CompareExchange(ref errors, ex, null);
                }
                Volatile.Write(ref done, true);
                Drain();
            }

            public void OnCompleted()
            {
                Volatile.Write(ref done, true);
                Drain();
            }

            public override void Dispose()
            {
                base.Dispose();
                DisposableHelper.Dispose(ref upstream);
                Drain();
            }

            internal override void DrainLoop()
            {
                var missed = 1;

                var sources = this.sources;
                var downstream = this.downstream;
                var maxConcurrency = this.maxConcurrency;

                for (; ; )
                {
                    if (IsDisposed())
                    {
                        while (sources.TryDequeue(out var _)) ;
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

                        var d = Volatile.Read(ref done);
                        var running = Volatile.Read(ref active);

                        if (!noMoreSources && running < maxConcurrency)
                        {

                            var src = default(IMaybeSource<R>);
                            var has = sources.TryDequeue(out var t);

                            if (has)
                            {
                                try
                                {
                                    src = RequireNonNullRef(mapper(t), "The mapper returned a null IMaybeSource");
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
                                    Volatile.Write(ref done, true);
                                }

                                if (!noMoreSources)
                                {
                                    Interlocked.Increment(ref active);

                                    SubscribeTo(src);
                                }
                                continue;
                            }
                            else
                            {
                                if (d)
                                {
                                    noMoreSources = true;
                                }
                            }
                        }

                        var q = GetQueue();

                        var v = default(R);

                        var empty = q == null || !q.TryDequeue(out v);

                        if (d && running == 0 && noMoreSources && empty)
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

            internal override void InnerSuccess(InnerObserver sender, R item)
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

        sealed class FlatMapAllCoordinator : MaybeMergeCoordinator<R>, IObserver<T>
        {
            readonly Func<T, IMaybeSource<R>> mapper;

            int active;

            IDisposable upstream;

            internal FlatMapAllCoordinator(IObserver<R> downstream, Func<T, IMaybeSource<R>> mapper, bool delayErrors) : base(downstream, delayErrors)
            {
                this.mapper = mapper;
                Volatile.Write(ref active, 1);
            }

            public void OnSubscribe(IDisposable d)
            {
                DisposableHelper.SetOnce(ref upstream, d);
            }

            public void OnNext(T item)
            {
                var src = default(IMaybeSource<R>);

                try
                {
                    src = RequireNonNullRef(mapper(item), "The mapper returned a null IMaybeSource");
                }
                catch (Exception ex)
                {
                    DisposableHelper.Dispose(ref upstream);
                    OnError(ex);
                    return;
                }

                Interlocked.Increment(ref active);
                SubscribeTo(src);
            }

            public void OnError(Exception ex)
            {
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

            public void OnCompleted()
            {
                Interlocked.Decrement(ref active);
                Drain();
            }

            public override void Dispose()
            {
                base.Dispose();
                DisposableHelper.Dispose(ref upstream);
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

                        var v = default(R);
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

            internal override void InnerSuccess(InnerObserver sender, R item)
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
