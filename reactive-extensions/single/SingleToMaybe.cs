using System;
using System.Collections.Generic;
using System.Text;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Converts a single source into a single source,
    /// failing with an index out-of-range exception
    /// if the single source is empty
    /// </summary>
    /// <typeparam name="T">The element type of the single source.</typeparam>
    /// <remarks>Since 0.0.12</remarks>
    internal sealed class SingleToMaybe<T> : IMaybeSource<T>
    {
        readonly ISingleSource<T> source;

        public SingleToMaybe(ISingleSource<T> source)
        {
            this.source = source;
        }

        public void Subscribe(IMaybeObserver<T> observer)
        {
            source.Subscribe(new ToObservableObserver(observer));
        }

        internal sealed class ToObservableObserver : ISingleObserver<T>, IDisposable
        {
            readonly IMaybeObserver<T> downstream;

            IDisposable upstream;

            public ToObservableObserver(IMaybeObserver<T> downstream)
            {
                this.downstream = downstream;
            }

            public void Dispose()
            {
                upstream.Dispose();
            }

            public void OnError(Exception error)
            {
                downstream.OnError(error);
            }

            public void OnSuccess(T item)
            {
                downstream.OnSuccess(item);
            }

            public void OnSubscribe(IDisposable d)
            {
                upstream = d;
                downstream.OnSubscribe(this);
            }
        }
    }
}
