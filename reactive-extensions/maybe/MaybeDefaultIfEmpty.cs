using System;
using System.Collections.Generic;
using System.Text;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// If the upstream maybe is empty, signal the default value
    /// as the success item to the downstream single observer.
    /// </summary>
    /// <typeparam name="T">The success value type.</typeparam>
    /// <remarks>Since 0.0.11</remarks>
    internal sealed class MaybeDefaultIfEmpty<T> : ISingleSource<T>
    {
        readonly IMaybeSource<T> source;

        readonly T defaultItem;

        public MaybeDefaultIfEmpty(IMaybeSource<T> source, T defaultItem)
        {
            this.source = source;
            this.defaultItem = defaultItem;
        }

        public void Subscribe(ISingleObserver<T> observer)
        {
            source.Subscribe(new DefaultIfEmptyObserver(observer, defaultItem));
        }

        sealed class DefaultIfEmptyObserver : IMaybeObserver<T>, IDisposable
        {
            readonly ISingleObserver<T> downstream;

            readonly T defaultItem;

            IDisposable upstream;

            public DefaultIfEmptyObserver(ISingleObserver<T> observer, T defaultItem)
            {
                this.downstream = observer;
                this.defaultItem = defaultItem;
            }

            public void Dispose()
            {
                upstream.Dispose();
            }

            public void OnCompleted()
            {
                downstream.OnSuccess(defaultItem);
            }

            public void OnError(Exception error)
            {
                downstream.OnError(error);
            }

            public void OnSubscribe(IDisposable d)
            {
                upstream = d;
                downstream.OnSubscribe(this);
            }

            public void OnSuccess(T item)
            {
                downstream.OnSuccess(item);
            }
        }
    }
}
