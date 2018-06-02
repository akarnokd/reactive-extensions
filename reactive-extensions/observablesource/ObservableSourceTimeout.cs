using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    internal sealed class ObservableSourceTimeout<T> : IObservableSource<T>
    {
        readonly IObservableSource<T> source;

        readonly TimeSpan timeout;

        readonly IScheduler scheduler;

        readonly IObservableSource<T> fallback;

        public ObservableSourceTimeout(IObservableSource<T> source, TimeSpan timeout, IScheduler scheduler, IObservableSource<T> fallback)
        {
            this.source = source;
            this.timeout = timeout;
            this.scheduler = scheduler;
            this.fallback = fallback;
        }

        public void Subscribe(ISignalObserver<T> observer)
        {
            var parent = new TimeoutObserver(observer, timeout, scheduler, fallback);
            observer.OnSubscribe(parent);

            parent.StartTimer(0);

            source.Subscribe(parent);
        }

        sealed class TimeoutObserver : ISignalObserver<T>, IDisposable
        {
            readonly ISignalObserver<T> downstream;

            readonly TimeSpan timeout;

            readonly IScheduler scheduler;

            IObservableSource<T> fallback;

            IDisposable upstream;

            IDisposable timer;

            IDisposable other;

            long index;

            public TimeoutObserver(ISignalObserver<T> downstream, TimeSpan timeout, IScheduler scheduler, IObservableSource<T> fallback)
            {
                this.downstream = downstream;
                this.timeout = timeout;
                this.scheduler = scheduler;
                this.fallback = fallback;
            }

            public void Dispose()
            {
                DisposableHelper.Dispose(ref upstream);
                DisposableHelper.Dispose(ref timer);
                DisposableHelper.Dispose(ref other);
            }

            public void OnCompleted()
            {
                if (Interlocked.Exchange(ref index, long.MaxValue) != long.MaxValue)
                {
                    DisposableHelper.Dispose(ref timer);

                    downstream.OnCompleted();
                }
            }

            public void OnError(Exception ex)
            {
                if (Interlocked.Exchange(ref index, long.MaxValue) != long.MaxValue)
                {
                    DisposableHelper.Dispose(ref timer);

                    downstream.OnError(ex);
                }
            }

            public void OnNext(T item)
            {
                var idx = Volatile.Read(ref index);
                if (idx != long.MaxValue && Interlocked.CompareExchange(ref index, idx + 1, idx) == idx)
                {
                    Volatile.Read(ref timer)?.Dispose();

                    downstream.OnNext(item);

                    StartTimer(idx + 1);
                }
            }

            public void OnSubscribe(IDisposable d)
            {
                DisposableHelper.SetOnce(ref upstream, d);
            }

            internal void StartTimer(long idx)
            {
                var d = scheduler.Schedule((@this: this, idx), timeout, (_, state) => state.@this.Timeout(idx));

                DisposableHelper.Replace(ref timer, d);
            }

            IDisposable Timeout(long idx)
            {
                if (Volatile.Read(ref index) == idx && Interlocked.CompareExchange(ref index, long.MaxValue, idx) == idx)
                {
                    DisposableHelper.Dispose(ref upstream);

                    var src = fallback;
                    fallback = null;

                    if (src != null)
                    {
                        var fo = new FallbackObserver(downstream);

                        if (DisposableHelper.Replace(ref other, fo))
                        {
                            src.Subscribe(fo);
                        }
                    }
                    else
                    {
                        downstream.OnError(new TimeoutException($"Timeout at index " + idx));
                    }
                }

                return DisposableHelper.EMPTY;
            }

            sealed class FallbackObserver : ISignalObserver<T>, IDisposable
            {
                readonly ISignalObserver<T> downstream;

                IDisposable upstream;

                public FallbackObserver(ISignalObserver<T> downstream)
                {
                    this.downstream = downstream;
                }

                public void Dispose()
                {
                    DisposableHelper.Dispose(ref upstream);
                }

                public void OnCompleted()
                {
                    downstream.OnCompleted();
                }

                public void OnError(Exception ex)
                {
                    downstream.OnError(ex);
                }

                public void OnNext(T item)
                {
                    downstream.OnNext(item);
                }

                public void OnSubscribe(IDisposable d)
                {
                    DisposableHelper.SetOnce(ref upstream, d);
                }
            }
        }
    }
}
