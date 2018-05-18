using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Delay the subscription to the actual source until
    /// the specified time elapses.
    /// </summary>
    /// <remarks>Since 0.0.9</remarks>
    internal sealed class CompletableDelaySubscriptionTime : ICompletableSource
    {
        readonly ICompletableSource source;

        readonly TimeSpan delay;

        readonly IScheduler scheduler;

        static readonly Func<IScheduler, DelaySubscriptionObserver, IDisposable> RUN =
            (s, t) => { t.Run(); return DisposableHelper.EMPTY; };

        public CompletableDelaySubscriptionTime(ICompletableSource source, TimeSpan delay, IScheduler scheduler)
        {
            this.source = source;
            this.delay = delay;
            this.scheduler = scheduler;
        }

        public void Subscribe(ICompletableObserver observer)
        {
            var parent = new DelaySubscriptionObserver(observer, source);
            observer.OnSubscribe(parent);

            parent.SetTask(scheduler.Schedule(parent, delay, RUN));
        }

        sealed class DelaySubscriptionObserver : ICompletableObserver, IDisposable
        {
            readonly ICompletableObserver downstream;

            ICompletableSource other;

            IDisposable upstream;

            public DelaySubscriptionObserver(ICompletableObserver downstream, ICompletableSource other)
            {
                this.downstream = downstream;
                this.other = other;
            }

            public void Dispose()
            {
                DisposableHelper.Dispose(ref upstream);
            }

            public void OnCompleted()
            {
                downstream.OnCompleted();
            }

            public void OnError(Exception error)
            {
                other = null;
                downstream.OnError(error);
            }

            public void OnSubscribe(IDisposable d)
            {
                DisposableHelper.Replace(ref upstream, d);
            }

            internal void SetTask(IDisposable d)
            {
                DisposableHelper.ReplaceIfNull(ref upstream, d);
            }

            internal void Run()
            {
                var o = other;
                other = null;

                o.Subscribe(this);
            }
        }
    }
    /// <summary>
    /// Delay the subscription to the actual source until
    /// the other source completes.
    /// </summary>
    /// <remarks>Since 0.0.9</remarks>
    internal sealed class CompletableDelaySubscription : ICompletableSource
    {
        readonly ICompletableSource source;

        readonly ICompletableSource other;

        public CompletableDelaySubscription(ICompletableSource source, ICompletableSource other)
        {
            this.source = source;
            this.other = other;
        }

        public void Subscribe(ICompletableObserver observer)
        {
            var parent = new DelaySubscriptionObserver(observer, source);
            observer.OnSubscribe(parent);
            other.Subscribe(parent);
        }

        sealed class DelaySubscriptionObserver : ICompletableObserver, IDisposable
        {
            readonly ICompletableObserver downstream;

            ICompletableSource other;

            IDisposable upstream;

            public DelaySubscriptionObserver(ICompletableObserver downstream, ICompletableSource other)
            {
                this.downstream = downstream;
                this.other = other;
            }

            public void Dispose()
            {
                DisposableHelper.Dispose(ref upstream);
            }

            public void OnCompleted()
            {
                if (other == null)
                {
                    downstream.OnCompleted();
                }
                else
                {
                    var o = other;
                    other = null;
                    o.Subscribe(this);
                }
            }

            public void OnError(Exception error)
            {
                other = null;
                downstream.OnError(error);
            }

            public void OnSubscribe(IDisposable d)
            {
                DisposableHelper.Replace(ref upstream, d);
            }

            internal void SetTask(IDisposable d)
            {
                DisposableHelper.ReplaceIfNull(ref upstream, d);
            }
        }
    }
}
