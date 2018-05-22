using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using static akarnokd.reactive_extensions.ValidationHelper;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Runs some or all single sources, provided by
    /// mapping the source observable sequence into
    /// single sources, at once but emits
    /// their success item in order and optionally delays
    /// errors until all sources terminate.
    /// </summary>
    /// <typeparam name="T">The value type of the source sequence.</typeparam>
    /// <typeparam name="R">The success value type of the inner single sources.</typeparam>
    /// <remarks>Since 0.0.12</remarks>
    internal sealed class SingleConcatMapEager<T, R> : IObservable<R>
    {
        readonly IObservable<T> source;

        readonly Func<T, ISingleSource<R>> mapper;

        readonly bool delayErrors;

        readonly int maxConcurrency;

        public SingleConcatMapEager(IObservable<T> source, Func<T, ISingleSource<R>> mapper, bool delayErrors, int maxConcurrency)
        {
            this.source = source;
            this.mapper = mapper;
            this.delayErrors = delayErrors;
            this.maxConcurrency = maxConcurrency;
        }

        public IDisposable Subscribe(IObserver<R> observer)
        {
            var parent = new ConcatEagerLimitedCoordinator(observer, mapper, maxConcurrency, delayErrors);

            parent.OnSubscribe(source.Subscribe(parent));

            return parent;
        }

        sealed class ConcatEagerLimitedCoordinator : SingleConcatEagerCoordinator<R>, IObserver<T>
        {
            readonly Func<T, ISingleSource<R>> mapper;

            readonly int maxConcurrency;

            readonly ConcurrentQueue<T> queue;

            int active;

            bool done;

            IDisposable upstream;

            bool noMoreSources;

            internal ConcatEagerLimitedCoordinator(IObserver<R> downstream, Func<T, ISingleSource<R>> mapper, int maxConcurrency, bool delayErrors) : base(downstream, delayErrors)
            {
                this.mapper = mapper;
                this.maxConcurrency = maxConcurrency;
                this.queue = new ConcurrentQueue<T>();
            }

            internal void OnSubscribe(IDisposable d)
            {
                DisposableHelper.SetOnce(ref upstream, d);
            }

            public void OnNext(T item)
            {
                queue.Enqueue(item);
                Drain();
            }

            public void OnError(Exception ex)
            {
                if (delayErrors)
                {
                    ExceptionHelper.AddException(ref errors, ex);
                    Volatile.Write(ref done, true);
                }
                else
                {
                    Interlocked.CompareExchange(ref errors, ex, null);
                }
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

            internal override void Drain()
            {
                if (Interlocked.Increment(ref wip) != 1)
                {
                    return;
                }

                var missed = 1;
                var observers = this.observers;
                var downstream = this.downstream;
                var delayErrors = this.delayErrors;
                var sources = this.queue;

                for (; ; )
                {
                    if (Volatile.Read(ref disposed))
                    {
                        while (observers.TryDequeue(out var inner))
                        {
                            inner.Dispose();
                        }

                        while (sources.TryDequeue(out var _)) ;
                    }
                    else
                    {
                        if (!delayErrors)
                        {
                            var ex = Volatile.Read(ref errors);
                            if (ex != null)
                            {
                                Volatile.Write(ref disposed, true);
                                downstream.OnError(ex);
                                DisposableHelper.Dispose(ref upstream);
                                continue;
                            }
                        }

                        if (!noMoreSources && active < maxConcurrency && !sources.IsEmpty)
                        {

                            if (sources.TryDequeue(out var v))
                            {
                                var src = default(ISingleSource<R>);
                                try
                                {
                                    src = RequireNonNullRef(mapper(v), "The mapper returned a null ISingleSource");
                                }
                                catch (Exception ex)
                                {
                                    if (delayErrors)
                                    {
                                        ExceptionHelper.AddException(ref errors, ex);
                                        Volatile.Write(ref done, true);

                                    }
                                    else
                                    {
                                        Interlocked.CompareExchange(ref errors, ex, null);
                                    }
                                    noMoreSources = true;
                                    DisposableHelper.Dispose(ref upstream);
                                    continue;
                                }

                                active++;

                                SubscribeTo(src);
                                continue;
                            }
                        }

                        var d = Volatile.Read(ref done) && sources.IsEmpty;
                        var empty = !observers.TryPeek(out var inner);

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
                            DisposableHelper.Dispose(ref upstream);
                        }
                        else
                        if (!empty)
                        {
                            var state = inner.GetState();

                            if (state == 1)
                            {
                                downstream.OnNext(inner.value);
                                observers.TryDequeue(out var _);
                                active--;
                                continue;
                            }
                            else
                            if (state == 2)
                            {
                                observers.TryDequeue(out var _);
                                active--;
                                continue;
                            }
                        }
                    }

                    missed = Interlocked.Add(ref wip, -missed);
                    if (missed == 0)
                    {
                        break;
                    }
                }
            }
        }
    }
}
