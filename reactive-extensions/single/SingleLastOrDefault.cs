using System;
using System.Collections.Generic;
using System.Text;

namespace akarnokd.reactive_extensions
{
    internal sealed class SingleLastOrDefault<T> : ISingleSource<T>
    {
        readonly IObservable<T> source;

        readonly T defaultItem;

        public SingleLastOrDefault(IObservable<T> source, T defaultItem)
        {
            this.source = source;
            this.defaultItem = defaultItem;
        }

        public void Subscribe(ISingleObserver<T> observer)
        {
            var parent = new LastOrDefault(observer, defaultItem);
            observer.OnSubscribe(parent);

            parent.OnSubscribe(source.Subscribe(parent));
        }

        sealed class LastOrDefault : IObserver<T>, IDisposable
        {
            readonly ISingleObserver<T> downstream;

            readonly T defaultItem;

            IDisposable upstream;

            T last;
            bool hasLast;

            public LastOrDefault(ISingleObserver<T> downstream, T defaultItem)
            {
                this.downstream = downstream;
                this.defaultItem = defaultItem;
            }

            public void Dispose()
            {
                DisposableHelper.Dispose(ref upstream);
            }

            public void OnCompleted()
            {
                if (hasLast)
                {
                    downstream.OnSuccess(last);
                }
                else
                {
                    downstream.OnSuccess(defaultItem);
                }
                Dispose();
            }

            public void OnError(Exception error)
            {
                downstream.OnError(error);
                Dispose();
            }

            public void OnNext(T value)
            {
                last = value;
                hasLast = true;
            }

            public void OnSubscribe(IDisposable d)
            {
                DisposableHelper.SetOnce(ref upstream, d);
            }
        }
    }
}
