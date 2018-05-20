using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// If the upstream doesn't terminate within the specified
    /// timeout, the maybe observer is terminated with
    /// a TimeoutException or is switched to the optional
    /// fallback maybe source.
    /// </summary>
    /// <typeparam name="T">The success value type.</typeparam>
    /// <remarks>Since 0.0.11</remarks>
    internal sealed class MaybeTimeout<T> : IMaybeSource<T>
    {

        readonly IMaybeSource<T> source;

        readonly TimeSpan timeout;

        readonly IScheduler scheduler;

        readonly IMaybeSource<T> fallback;

        static readonly Func<IScheduler, TimeoutObserver, IDisposable> RUN =
            (s, t) => { t.Run(); return DisposableHelper.EMPTY; };

        public MaybeTimeout(IMaybeSource<T> source, TimeSpan timeout, IScheduler scheduler, IMaybeSource<T> fallback)
        {
            this.source = source;
            this.timeout = timeout;
            this.scheduler = scheduler;
            this.fallback = fallback;
        }

        public void Subscribe(IMaybeObserver<T> observer)
        {
            var parent = new TimeoutObserver(observer, fallback);
            observer.OnSubscribe(parent);
            parent.SetTask(scheduler.Schedule(parent, timeout, RUN));
            source.Subscribe(parent);
        }

        sealed class TimeoutObserver : IMaybeObserver<T>, IDisposable
        {
            readonly IMaybeObserver<T> downstream;

            IDisposable upstream;

            IDisposable task;

            IMaybeSource<T> fallback;

            IDisposable fallbackObserver;

            int exclude;

            public TimeoutObserver(IMaybeObserver<T> downstream, IMaybeSource<T> fallback)
            {
                this.downstream = downstream;
                this.fallback = fallback;
            }

            public void Dispose()
            {
                if (Interlocked.CompareExchange(ref exclude, 1, 0) == 0)
                {
                    DisposableHelper.Dispose(ref upstream);
                    DisposableHelper.Dispose(ref task);
                }
                DisposableHelper.Dispose(ref fallbackObserver);
            }

            internal void SetTask(IDisposable d)
            {
                DisposableHelper.Replace(ref task, d);
            }

            public void OnCompleted()
            {
                if (Interlocked.CompareExchange(ref exclude, 1, 0) == 0)
                {
                    DisposableHelper.Dispose(ref task);

                    downstream.OnCompleted();
                }
            }

            public void OnError(Exception error)
            {
                if (Interlocked.CompareExchange(ref exclude, 1, 0) == 0)
                {
                    DisposableHelper.Dispose(ref task);

                    downstream.OnError(error);
                }
            }

            public void OnSuccess(T item)
            {
                if (Interlocked.CompareExchange(ref exclude, 1, 0) == 0)
                {
                    DisposableHelper.Dispose(ref task);

                    downstream.OnSuccess(item);
                }
            }

            public void OnSubscribe(IDisposable d)
            {
                DisposableHelper.SetOnce(ref upstream, d);
            }

            internal void Run()
            {
                if (Interlocked.CompareExchange(ref exclude, 1, 0) == 0)
                {
                    DisposableHelper.Dispose(ref upstream);

                    var c = fallback;

                    if (c == null)
                    {
                        downstream.OnError(new TimeoutException());
                    }
                    else
                    {
                        fallback = null;

                        var inner = new MaybeInnerObserver<T>(downstream);
                        if (Interlocked.CompareExchange(ref fallbackObserver, inner, null) == null)
                        {
                            c.Subscribe(inner);
                        }
                    }
                }
            }
        }
    }
}
