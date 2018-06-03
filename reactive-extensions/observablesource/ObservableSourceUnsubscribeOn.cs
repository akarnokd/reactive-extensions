using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    internal sealed class ObservableSourceUnsubscribeOn<T> : IObservableSource<T>
    {
        readonly IObservableSource<T> source;

        readonly IScheduler scheduler;

        public ObservableSourceUnsubscribeOn(IObservableSource<T> source, IScheduler scheduler)
        {
            this.source = source;
            this.scheduler = scheduler;
        }

        public void Subscribe(ISignalObserver<T> observer)
        {
            source.Subscribe(new UnsubscribeOnObserver(observer, scheduler));
        }

        sealed class UnsubscribeOnObserver : ISignalObserver<T>, IDisposable
        {
            readonly ISignalObserver<T> downstream;

            readonly IScheduler scheduler;

            IDisposable upstream;

            public UnsubscribeOnObserver(ISignalObserver<T> downstream, IScheduler scheduler)
            {
                this.downstream = downstream;
                this.scheduler = scheduler;
            }

            public void Dispose()
            {
                var d = upstream;
                if (Interlocked.CompareExchange(ref upstream, DisposableHelper.DISPOSED, d) == d)
                {
                    scheduler.Schedule(d, (_, state) =>
                    {
                        state.Dispose();
                        return DisposableHelper.EMPTY;
                    });
                }
            }

            public void OnCompleted()
            {
                if (upstream != DisposableHelper.DISPOSED)
                {
                    DisposableHelper.WeakDispose(ref upstream);
                    downstream.OnCompleted();
                }
            }

            public void OnError(Exception ex)
            {
                if (upstream != DisposableHelper.DISPOSED)
                {
                    DisposableHelper.WeakDispose(ref upstream);
                    downstream.OnError(ex);
                }
            }

            public void OnNext(T item)
            {
                if (upstream != DisposableHelper.DISPOSED)
                {
                    downstream.OnNext(item);
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
