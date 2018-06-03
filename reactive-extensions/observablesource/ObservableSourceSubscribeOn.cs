using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Text;

namespace akarnokd.reactive_extensions
{
    internal sealed class ObservableSourceSubscribeOn<T> : IObservableSource<T>
    {
        readonly IObservableSource<T> source;

        readonly IScheduler scheduler;

        public ObservableSourceSubscribeOn(IObservableSource<T> source, IScheduler scheduler)
        {
            this.source = source;
            this.scheduler = scheduler;
        }

        public void Subscribe(ISignalObserver<T> observer)
        {
            var parent = new SubscribeOnObserver(observer);
            observer.OnSubscribe(parent);

            var d = scheduler.Schedule((source, parent), (_, t) =>
            {
                t.source.Subscribe(t.parent);
                return DisposableHelper.EMPTY;
            });

            parent.SetTask(d);
        }

        sealed class SubscribeOnObserver : ISignalObserver<T>, IDisposable
        {
            readonly ISignalObserver<T> downstream;

            IDisposable upstream;

            public SubscribeOnObserver(ISignalObserver<T> downstream)
            {
                this.downstream = downstream;
            }

            internal void SetTask(IDisposable d)
            {
                DisposableHelper.ReplaceIfNull(ref upstream, d);
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
                DisposableHelper.Replace(ref upstream, d);
            }
        }
    }
}
