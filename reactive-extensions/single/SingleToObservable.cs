using System;
using System.Collections.Generic;
using System.Text;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Exposes a single source as a legacy observable.
    /// </summary>
    /// <typeparam name="T">The element type of the observable sequence.</typeparam>
    /// <remarks>Since 0.0.12</remarks>
    internal sealed class SingleToObservable<T> : IObservable<T>
    {
        readonly ISingleSource<T> source;

        public SingleToObservable(ISingleSource<T> source)
        {
            this.source = source;
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            var parent = new ToObservableObserver(observer);
            source.Subscribe(parent);
            return parent;
        }

        internal sealed class ToObservableObserver : ISingleObserver<T>, IDisposable
        {
            readonly IObserver<T> downstream;

            IDisposable upstream;

            public ToObservableObserver(IObserver<T> downstream)
            {
                this.downstream = downstream;
            }

            public void Dispose()
            {
                DisposableHelper.Dispose(ref upstream);
            }

            public void OnError(Exception error)
            {
                DisposableHelper.WeakDispose(ref upstream);
                downstream.OnError(error);
            }

            public void OnSuccess(T item)
            {
                DisposableHelper.WeakDispose(ref upstream);
                downstream.OnNext(item);
                downstream.OnCompleted();
            }

            public void OnSubscribe(IDisposable d)
            {
                DisposableHelper.SetOnce(ref upstream, d);
            }
        }
    }
}
