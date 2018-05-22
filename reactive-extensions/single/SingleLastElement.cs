using System;
using System.Collections.Generic;
using System.Text;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Signals the last element of the observable sequence
    /// or completes if the sequence is empty.
    /// </summary>
    /// <typeparam name="T">The value type of the source observable.</typeparam>
    /// <remarks>Since 0.0.11</remarks>
    internal sealed class SingleLastElement<T> : ISingleSource<T>
    {
        readonly IObservable<T> source;

        public SingleLastElement(IObservable<T> source)
        {
            this.source = source;
        }

        public void Subscribe(ISingleObserver<T> observer)
        {
            var parent = new LastElementObserver(observer);
            observer.OnSubscribe(parent);

            parent.OnSubscribe(source.Subscribe(parent));
        }

        sealed class LastElementObserver : IObserver<T>, IDisposable
        {
            readonly ISingleObserver<T> downstream;

            IDisposable upstream;

            T element;
            bool hasElement;

            public LastElementObserver(ISingleObserver<T> downstream)
            {
                this.downstream = downstream;
            }

            public void Dispose()
            {
                DisposableHelper.Dispose(ref upstream);
            }

            public void OnCompleted()
            {
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
            }

            public void OnError(Exception error)
            {
                element = default(T);
                downstream.OnError(error);
            }

            public void OnNext(T value)
            {
                element = value;
                hasElement = true;
            }

            public void OnSubscribe(IDisposable d)
            {
                DisposableHelper.SetOnce(ref upstream, d);
            }
        }
    }
}
