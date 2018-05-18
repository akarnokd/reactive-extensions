using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// If the upstream doesn't terminate within the specified
    /// timeout, the completable observer is terminated with
    /// a TimeoutException or is switched to the optional
    /// fallback completable source.
    /// </summary>
    /// <remarks>Since 0.0.8</remarks>
    internal sealed class CompletableTimeout : ICompletableSource
    {

        readonly ICompletableSource source;

        readonly TimeSpan timeout;

        readonly IScheduler scheduler;

        readonly ICompletableSource fallback;

        static readonly Func<IScheduler, TimeoutObserver, IDisposable> RUN =
            (s, t) => { t.Run(); return DisposableHelper.EMPTY; };

        public CompletableTimeout(ICompletableSource source, TimeSpan timeout, IScheduler scheduler, ICompletableSource fallback)
        {
            this.source = source;
            this.timeout = timeout;
            this.scheduler = scheduler;
            this.fallback = fallback;
        }

        public void Subscribe(ICompletableObserver observer)
        {
            var parent = new TimeoutObserver(observer, fallback);
            observer.OnSubscribe(parent);
            parent.SetTask(scheduler.Schedule(parent, timeout, RUN));
            source.Subscribe(parent);
        }

        sealed class TimeoutObserver : ICompletableObserver, IDisposable
        {
            readonly ICompletableObserver downstream;

            IDisposable upstream;

            IDisposable task;

            ICompletableSource fallback;

            IDisposable fallbackObserver;

            int exclude;

            public TimeoutObserver(ICompletableObserver downstream, ICompletableSource fallback)
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

                        var inner = new CompletableInnerObserver(downstream);
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
