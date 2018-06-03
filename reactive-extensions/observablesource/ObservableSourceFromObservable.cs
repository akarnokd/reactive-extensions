using System;
using System.Collections.Generic;
using System.Text;

namespace akarnokd.reactive_extensions
{
    internal sealed class ObservableSourceFromObservable<T> : IObservableSource<T>
    {
        readonly IObservable<T> source;

        public ObservableSourceFromObservable(IObservable<T> source)
        {
            this.source = source;
        }

        public void Subscribe(ISignalObserver<T> observer)
        {
            var parent = new FromObserver(observer);
            observer.OnSubscribe(parent);

            parent.OnSubscribe(source.Subscribe(parent));
        }

        sealed class FromObserver : IObserver<T>, IDisposable
        {
            readonly ISignalObserver<T> downstream;

            IDisposable upstream;

            public FromObserver(ISignalObserver<T> downstream)
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
                Dispose();
            }

            public void OnError(Exception error)
            {
                downstream.OnError(error);
                Dispose();
            }

            public void OnNext(T value)
            {
                downstream.OnNext(value);
            }

            internal void OnSubscribe(IDisposable d)
            {
                DisposableHelper.SetOnce(ref upstream, d);
            }
        }
    }
}
