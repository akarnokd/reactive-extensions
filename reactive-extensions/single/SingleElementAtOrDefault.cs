using System;
using System.Collections.Generic;
using System.Text;

namespace akarnokd.reactive_extensions
{
    internal sealed class SingleElementAtOrDefault<T> : ISingleSource<T>
    {
        readonly IObservable<T> source;

        readonly long index;

        readonly T defaultItem;

        public SingleElementAtOrDefault(IObservable<T> source, long index, T defaultItem)
        {
            this.source = source;
            this.index = index;
            this.defaultItem = defaultItem;
        }

        public void Subscribe(ISingleObserver<T> observer)
        {
            var parent = new ElementAtOrDefault(observer, index, defaultItem);

            parent.OnSubscribe(source.Subscribe(parent));
        }

        sealed class ElementAtOrDefault : IObserver<T>, IDisposable
        {
            readonly ISingleObserver<T> downstream;

            readonly T defaultItem;

            IDisposable upstream;

            long index;

            public ElementAtOrDefault(ISingleObserver<T> downstream, long index, T defaultItem)
            {
                this.downstream = downstream;
                this.index = index;
                this.defaultItem = defaultItem;
            }

            public void Dispose()
            {
                DisposableHelper.Dispose(ref upstream);
            }

            public void OnSubscribe(IDisposable d)
            {
                DisposableHelper.SetOnce(ref upstream, d);
            }

            public void OnCompleted()
            {
                if (index >= 0L)
                {
                    downstream.OnSuccess(defaultItem);
                    Dispose();
                }
            }

            public void OnError(Exception error)
            {
                downstream.OnError(error);
                Dispose();
            }

            public void OnNext(T value)
            {
                var idx = index;

                if (idx == 0L)
                {
                    downstream.OnSuccess(value);
                    Dispose();
                }
                index = idx - 1;
            }
        }
    }
}
