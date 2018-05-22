using System;
using System.Collections.Generic;
using System.Text;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Signals the element at the specified index as the
    /// success value or completes if the observable
    /// sequence is shorter than the specified index.
    /// </summary>
    /// <typeparam name="T">The element type of the sequence.</typeparam>
    /// <remarks>Since 0.0.11</remarks>
    internal sealed class SingleElementAtOrError<T> : ISingleSource<T>
    {
        readonly IObservable<T> source;

        readonly long index;

        public SingleElementAtOrError(IObservable<T> source, long index)
        {
            this.source = source;
            this.index = index;
        }

        public void Subscribe(ISingleObserver<T> observer)
        {
            var parent = new ElementAtObserver(observer, index);
            observer.OnSubscribe(parent);

            parent.OnSubscribe(source.Subscribe(parent));
        }

        sealed class ElementAtObserver : IObserver<T>, IDisposable
        {
            readonly ISingleObserver<T> downstream;

            long index;

            IDisposable upstream;

            public ElementAtObserver(ISingleObserver<T> downstream, long index)
            {
                this.downstream = downstream;
                this.index = index;
            }

            public void Dispose()
            {
                DisposableHelper.Dispose(ref upstream);
            }

            public void OnCompleted()
            {
                if (index >= 0)
                {
                    downstream.OnError(new IndexOutOfRangeException("The source is empty"));
                }
                Dispose();
            }

            public void OnError(Exception error)
            {
                if (index >= 0)
                {
                    downstream.OnError(error);
                }
                Dispose();
            }

            public void OnNext(T value)
            {
                if (index-- == 0)
                {
                    downstream.OnSuccess(value);
                    Dispose();
                }
            }

            internal void OnSubscribe(IDisposable d)
            {
                DisposableHelper.SetOnce(ref upstream, d);
            }
        }
    }
}
