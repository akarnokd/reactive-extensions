using System;
using System.Collections.Generic;
using System.Text;

namespace akarnokd.reactive_extensions
{
    internal sealed class ObservableSourceToObservable<T> : IObservable<T>
    {
        readonly IObservableSource<T> source;

        public ObservableSourceToObservable(IObservableSource<T> source)
        {
            this.source = source;
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            var parent = new FromObserver(observer);
            source.Subscribe(parent);
            return parent;
        }

        sealed class FromObserver : ISignalObserver<T>, IDisposable
        {
            readonly IObserver<T> downstream;

            IDisposable upstream;

            public FromObserver(IObserver<T> downstream)
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

            public void OnError(Exception error)
            {
                downstream.OnError(error);
            }

            public void OnNext(T value)
            {
                downstream.OnNext(value);
            }

            public void OnSubscribe(IDisposable d)
            {
                DisposableHelper.SetOnce(ref upstream, d);
            }
        }
    }
}
