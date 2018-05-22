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
    /// <typeparam name="T">The success value type.</typeparam>
    /// <remarks>Since 0.0.11</remarks>
    internal sealed class SingleDelaySubscriptionTime<T> : ISingleSource<T>
    {
        readonly ISingleSource<T> source;

        readonly TimeSpan delay;

        readonly IScheduler scheduler;

        static readonly Func<IScheduler, DelaySubscriptionObserver, IDisposable> RUN =
            (s, t) => { t.Run(); return DisposableHelper.EMPTY; };

        public SingleDelaySubscriptionTime(ISingleSource<T> source, TimeSpan delay, IScheduler scheduler)
        {
            this.source = source;
            this.delay = delay;
            this.scheduler = scheduler;
        }

        public void Subscribe(ISingleObserver<T> observer)
        {
            var parent = new DelaySubscriptionObserver(observer, source);
            observer.OnSubscribe(parent);

            parent.SetTask(scheduler.Schedule(parent, delay, RUN));
        }

        sealed class DelaySubscriptionObserver : ISingleObserver<T>, IDisposable
        {
            readonly ISingleObserver<T> downstream;

            ISingleSource<T> other;

            IDisposable upstream;

            public DelaySubscriptionObserver(ISingleObserver<T> downstream, ISingleSource<T> other)
            {
                this.downstream = downstream;
                this.other = other;
            }

            public void Dispose()
            {
                DisposableHelper.Dispose(ref upstream);
            }

            public void OnError(Exception error)
            {
                downstream.OnError(error);
            }

            public void OnSuccess(T item)
            {
                downstream.OnSuccess(item);
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
    /// <typeparam name="T">The success value type.</typeparam>
    /// <typeparam name="U">The value type of the other source triggering the
    /// subscription to the main source.</typeparam>
    /// <remarks>Since 0.0.11</remarks>
    internal sealed class SingleDelaySubscription<T, U> : ISingleSource<T>
    {
        readonly ISingleSource<T> source;

        readonly ISingleSource<U> other;

        public SingleDelaySubscription(ISingleSource<T> source, ISingleSource<U> other)
        {
            this.source = source;
            this.other = other;
        }

        public void Subscribe(ISingleObserver<T> observer)
        {
            var parent = new DelaySubscriptionObserver(observer, source);
            observer.OnSubscribe(parent);
            other.Subscribe(parent);
        }

        sealed class DelaySubscriptionObserver : ISingleObserver<U>, IDisposable
        {
            readonly ISingleObserver<T> downstream;

            ISingleSource<T> main;

            IDisposable upstream;

            public DelaySubscriptionObserver(ISingleObserver<T> downstream, ISingleSource<T> main)
            {
                this.downstream = downstream;
                this.main = main;
            }

            public void Dispose()
            {
                DisposableHelper.Dispose(ref upstream);
            }

            public void OnError(Exception error)
            {
                main = null;
                downstream.OnError(error);
            }

            public void OnSuccess(U item)
            {
                Next();
            }

            void Next()
            {
                var o = main;
                main = null;

                var inner = new SingleInnerObserver<T>(downstream);
                if (DisposableHelper.Replace(ref upstream, inner))
                {
                    o.Subscribe(inner);
                }
            }

            public void OnSubscribe(IDisposable d)
            {
                DisposableHelper.SetOnce(ref upstream, d);
            }

            internal void SetTask(IDisposable d)
            {
                DisposableHelper.ReplaceIfNull(ref upstream, d);
            }
        }
    }
}
