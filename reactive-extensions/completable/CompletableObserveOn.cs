using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Signals the terminal events of the completable source
    /// through the specified scheduler.
    /// </summary>
    /// <remarks>Since 0.0.8</remarks>
    internal sealed class CompletableObserveOn : ICompletableSource
    {
        readonly ICompletableSource source;

        readonly IScheduler scheduler;

        public CompletableObserveOn(ICompletableSource source, IScheduler scheduler)
        {
            this.source = source;
            this.scheduler = scheduler;
        }

        public void Subscribe(ICompletableObserver observer)
        {
            source.Subscribe(new ObserveOnObserver(observer, scheduler));
        }

        sealed class ObserveOnObserver : ICompletableObserver, IDisposable
        {
            readonly ICompletableObserver downstream;

            readonly IScheduler scheduler;

            IDisposable upstream;

            IDisposable task;

            Exception error;

            static readonly Func<IScheduler, ObserveOnObserver, IDisposable> RUN =
                (s, t) => { t.Run(); return DisposableHelper.EMPTY; };

            public ObserveOnObserver(ICompletableObserver downstream, IScheduler scheduler)
            {
                this.downstream = downstream;
                this.scheduler = scheduler;
            }

            public void Dispose()
            {
                upstream.Dispose();
                DisposableHelper.Dispose(ref task);
            }

            public void OnCompleted()
            {
                Schedule();
            }

            public void OnError(Exception error)
            {
                this.error = error;
                Schedule();
            }

            void Schedule()
            {
                var d = Volatile.Read(ref task);
                if (d != DisposableHelper.DISPOSED)
                {
                    var u = scheduler.Schedule(this, RUN);

                    if (Interlocked.CompareExchange(ref task, u, d) == DisposableHelper.DISPOSED)
                    {
                        u.Dispose();
                    }
                }
            }

            public void OnSubscribe(IDisposable d)
            {
                upstream = d;
                downstream.OnSubscribe(this);
            }

            internal void Run()
            {
                for (; ; )
                {
                    var d = Volatile.Read(ref task);
                    if (d == DisposableHelper.DISPOSED)
                    {
                        break;
                    }
                    if (Interlocked.CompareExchange(ref task, DisposableHelper.EMPTY, d) == d)
                    {
                        var ex = error;
                        if (ex != null)
                        {
                            downstream.OnError(ex);
                        }
                        else
                        {
                            downstream.OnCompleted();
                        }
                        break;
                    }
                }
            }
        }
    }
}
