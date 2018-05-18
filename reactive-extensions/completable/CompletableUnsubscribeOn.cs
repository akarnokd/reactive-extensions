using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// When the downstream disposes, the upstream's disposable
    /// is called from the given scheduler.
    /// </summary>
    /// <remarks>Since 0.0.8</remarks>
    internal sealed class CompletableUnsubscribeOn : ICompletableSource
    {
        readonly ICompletableSource source;

        readonly IScheduler scheduler;

        public CompletableUnsubscribeOn(ICompletableSource source, IScheduler scheduler)
        {
            this.source = source;
            this.scheduler = scheduler;
        }

        public void Subscribe(ICompletableObserver observer)
        {
            source.Subscribe(new UnsubscribeOnObserver(observer, scheduler));
        }

        sealed class UnsubscribeOnObserver : ICompletableObserver, IDisposable
        {
            readonly ICompletableObserver downstream;

            readonly IScheduler scheduler;

            int disposed;

            IDisposable upstream;

            static readonly Func<IScheduler, UnsubscribeOnObserver, IDisposable> RUN =
                (s, t) => { t.Run(); return DisposableHelper.EMPTY; };

            public UnsubscribeOnObserver(ICompletableObserver downstream, IScheduler scheduler)
            {
                this.downstream = downstream;
                this.scheduler = scheduler;
            }

            public void Dispose()
            {
                if (Interlocked.Exchange(ref disposed, 1) == 0)
                {
                    scheduler.Schedule(this, RUN);
                }
            }

            void Run()
            {
                upstream.Dispose();
            }

            public void OnCompleted()
            {
                if (Volatile.Read(ref disposed) == 0)
                {
                    downstream.OnCompleted();
                }
            }

                public void OnError(Exception error)
            {
                if (Volatile.Read(ref disposed) == 0)
                {
                    downstream.OnError(error);
                }
            }

            public void OnSubscribe(IDisposable d)
            {
                upstream = d;
                downstream.OnSubscribe(this);
            }
        }
    }
}
