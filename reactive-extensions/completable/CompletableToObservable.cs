using System;
using System.Collections.Generic;
using System.Text;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Exposes a completable source as a legacy observable.
    /// </summary>
    /// <typeparam name="T">The element type of the observable sequence.</typeparam>
    /// <remarks>Since 0.0.6</remarks>
    internal sealed class CompletableToObservable<T> : IObservable<T>
    {
        readonly ICompletableSource source;

        public CompletableToObservable(ICompletableSource source)
        {
            this.source = source;
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            var parent = new ToObservableObserver(observer);
            source.Subscribe(parent);
            return parent;
        }

        internal sealed class ToObservableObserver : ICompletableObserver, IDisposable
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

            public void OnSubscribe(IDisposable d)
            {
                DisposableHelper.SetOnce(ref upstream, d);
            }
        }
    }
}
