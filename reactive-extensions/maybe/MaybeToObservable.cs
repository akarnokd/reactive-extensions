using System;
using System.Collections.Generic;
using System.Text;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Exposes a maybe source as a legacy observable.
    /// </summary>
    /// <typeparam name="T">The element type of the observable sequence.</typeparam>
    /// <remarks>Since 0.0.12</remarks>
    internal sealed class MaybeToObservable<T> : IObservable<T>
    {
        readonly IMaybeSource<T> source;

        public MaybeToObservable(IMaybeSource<T> source)
        {
            this.source = source;
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            var parent = new ToObservableObserver(observer);
            source.Subscribe(parent);
            return parent;
        }

        internal sealed class ToObservableObserver : IMaybeObserver<T>, IDisposable
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

            public void OnCompleted()
            {
                DisposableHelper.WeakDispose(ref upstream);
                downstream.OnCompleted();
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
