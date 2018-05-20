using System;
using System.Collections.Generic;
using System.Text;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Converts a maybe source into a single source,
    /// failing with an index out-of-range exception
    /// if the maybe source is empty
    /// </summary>
    /// <typeparam name="T">The element type of the maybe source.</typeparam>
    /// <remarks>Since 0.0.12</remarks>
    internal sealed class MaybeToSingle<T> : ISingleSource<T>
    {
        readonly IMaybeSource<T> source;

        public MaybeToSingle(IMaybeSource<T> source)
        {
            this.source = source;
        }

        public void Subscribe(ISingleObserver<T> observer)
        {
            source.Subscribe(new ToObservableObserver(observer));
        }

        internal sealed class ToObservableObserver : IMaybeObserver<T>, IDisposable
        {
            readonly ISingleObserver<T> downstream;

            IDisposable upstream;

            public ToObservableObserver(ISingleObserver<T> downstream)
            {
                this.downstream = downstream;
            }

            public void Dispose()
            {
                upstream.Dispose();
            }

            public void OnCompleted()
            {
                downstream.OnError(new IndexOutOfRangeException("Empty IMaybeSource"));
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
