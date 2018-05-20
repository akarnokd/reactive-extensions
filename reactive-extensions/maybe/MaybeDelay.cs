using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Signals the terminal events of the completable source
    /// through the specified scheduler after a time delay.
    /// </summary>
    /// <typeparam name="T">The success value type.</typeparam>
    /// <remarks>Since 0.0.11</remarks>
    internal sealed class MaybeDelay<T> : IMaybeSource<T>
    {
        readonly IMaybeSource<T> source;

        readonly TimeSpan delay;

        readonly IScheduler scheduler;

        public MaybeDelay(IMaybeSource<T> source, TimeSpan delay, IScheduler scheduler)
        {
            this.source = source;
            this.delay = delay;
            this.scheduler = scheduler;
        }

        public void Subscribe(IMaybeObserver<T> observer)
        {
            source.Subscribe(new ObserveOnObserver(observer, delay, scheduler));
        }

        sealed class ObserveOnObserver : IMaybeObserver<T>, IDisposable
        {
            readonly IMaybeObserver<T> downstream;

            readonly TimeSpan delay;

            readonly IScheduler scheduler;

            IDisposable upstream;

            IDisposable task;

            Exception error;

            T value;
            bool hasValue;

            static readonly Func<IScheduler, ObserveOnObserver, IDisposable> RUN =
                (s, t) => { t.Run(); return DisposableHelper.EMPTY; };

            public ObserveOnObserver(IMaybeObserver<T> downstream, TimeSpan delay, IScheduler scheduler)
            {
                this.downstream = downstream;
                this.delay = delay;
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

            public void OnSuccess(T item)
            {
                value = item;
                hasValue = true;
                Schedule();
            }

            void Schedule()
            {
                var d = Volatile.Read(ref task);
                if (d != DisposableHelper.DISPOSED)
                {
                    var u = scheduler.Schedule(this, delay, RUN);

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
                var d = Volatile.Read(ref task);
                if (d != DisposableHelper.DISPOSED
                    && Interlocked.CompareExchange(ref task, DisposableHelper.EMPTY, d) == d)
                {
                    var ex = error;
                    if (ex != null)
                    {
                        downstream.OnError(ex);
                    }
                    else
                    {
                        if (hasValue)
                        {
                            downstream.OnSuccess(value);
                        }
                        else
                        {
                            downstream.OnCompleted();
                        }
                    }
                }
            }
        }
    }
}
