using System;
using System.Collections.Generic;
using System.Text;

namespace akarnokd.reactive_extensions
{
    internal sealed class ObservableSourceIsEmpty<T> : IObservableSource<bool>
    {
        readonly IObservableSource<T> source;

        public ObservableSourceIsEmpty(IObservableSource<T> source)
        {
            this.source = source;
        }

        public void Subscribe(ISignalObserver<bool> observer)
        {
            source.Subscribe(new IsEmptyObserver(observer));
        }

        sealed class IsEmptyObserver : DeferredScalarDisposable<bool>, ISignalObserver<T>
        {
            IDisposable upstream;

            internal IsEmptyObserver(ISignalObserver<bool> downstream) : base(downstream)
            {
            }

            public void OnCompleted()
            {
                Complete(true);
            }

            public void OnError(Exception ex)
            {
                Error(ex);
            }

            public void OnNext(T item)
            {
                upstream.Dispose();
                Complete(false);
            }

            public void OnSubscribe(IDisposable d)
            {
                upstream = d;
                downstream.OnSubscribe(this);
            }

            public override void Dispose()
            {
                base.Dispose();
                upstream.Dispose();
            }
        }
    }
}
