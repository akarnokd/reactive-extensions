using System;
using System.Collections.Generic;
using System.Text;

namespace akarnokd.reactive_extensions
{
    internal sealed class ObservableSourceHide<T> : IObservableSource<T>
    {
        readonly IObservableSource<T> source;

        public ObservableSourceHide(IObservableSource<T> source)
        {
            this.source = source;
        }

        public void Subscribe(ISignalObserver<T> observer)
        {
            source.Subscribe(new HideObserver(observer));
        }

        sealed class HideObserver : ISignalObserver<T>, IDisposable
        {
            readonly ISignalObserver<T> downstream;

            IDisposable upstream;

            public HideObserver(ISignalObserver<T> downstream)
            {
                this.downstream = downstream;
            }

            public void Dispose()
            {
                upstream.Dispose();
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
                upstream = d;
                downstream.OnSubscribe(this);
            }
        }
    }
}
