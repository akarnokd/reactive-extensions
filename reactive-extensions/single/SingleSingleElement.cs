using System;
using System.Collections.Generic;
using System.Text;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Signals the only element of the observable sequence,
    /// completes if the sequence is empty or fails
    /// with an error if the source contains more than one element.
    /// </summary>
    /// <typeparam name="T">The value type of the source observable.</typeparam>
    /// <remarks>Since 0.0.11</remarks>
    internal sealed class SingleSingleElement<T> : ISingleSource<T>
    {
        readonly IObservable<T> source;

        public SingleSingleElement(IObservable<T> source)
        {
            this.source = source;
        }

        public void Subscribe(ISingleObserver<T> observer)
        {
            var parent = new SingleElementObserver(observer);
            observer.OnSubscribe(parent);

            parent.OnSubscribe(source.Subscribe(parent));
        }

        sealed class SingleElementObserver : IObserver<T>, IDisposable
        {
            readonly ISingleObserver<T> downstream;

            IDisposable upstream;

            T element;
            bool hasElement;

            bool done;

            public SingleElementObserver(ISingleObserver<T> downstream)
            {
                this.downstream = downstream;
            }

            public void Dispose()
            {
                DisposableHelper.Dispose(ref upstream);
            }

            public void OnCompleted()
            {
                if (done)
                {
                    return;
                }
                if (hasElement)
                {
                    var e = element;
                    element = default(T);

                    downstream.OnSuccess(e);
                }
                else
                {
                    downstream.OnError(new IndexOutOfRangeException("The source is empty"));
                }
                Dispose();
            }

            public void OnError(Exception error)
            {
                if (done)
                {
                    return;
                }
                element = default(T);
                downstream.OnError(error);
                Dispose();
            }

            public void OnNext(T value)
            {
                if (done)
                {
                    return;
                }

                if (hasElement)
                {
                    element = default(T);
                    done = true;
                    downstream.OnError(new IndexOutOfRangeException("The source emitted more than one item"));
                    Dispose();
                }
                else
                {
                    hasElement = true;
                    element = value;
                }
            }

            public void OnSubscribe(IDisposable d)
            {
                DisposableHelper.SetOnce(ref upstream, d);
            }
        }
    }
}
