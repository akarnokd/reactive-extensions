using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Ignores the elements of a legacy observable and only relays
    /// the terminal events.
    /// </summary>
    /// <typeparam name="T">The element type of the legacy observable.</typeparam>
    internal sealed class CompletableIgnoreAllElements<T> : ICompletableSource
    {
        readonly IObservable<T> source;

        public CompletableIgnoreAllElements(IObservable<T> source)
        {
            this.source = source;
        }

        public void Subscribe(ICompletableObserver observer)
        {
            var parent = new IgnoreAllElementsObserver(observer);
            observer.OnSubscribe(parent);
            parent.OnSubscribe(source.Subscribe(parent));
        }

        sealed class IgnoreAllElementsObserver : IObserver<T>, ICompletableObserver, IDisposable
        {
            readonly ICompletableObserver downstream;

            IDisposable upstream;

            public IgnoreAllElementsObserver(ICompletableObserver downstream)
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

            public void OnNext(T value)
            {
                // deliberately ignored
            }

            public void OnSubscribe(IDisposable d)
            {
                DisposableHelper.SetOnce(ref upstream, d);
            }
        }
    }
}
