using System;
using System.Collections.Generic;
using System.Text;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// When the upstream completable source completes, the
    /// downstream single observer receives a success item.
    /// </summary>
    /// <typeparam name="T">The type of the success item.</typeparam>
    /// <remarks>Since 0.0.9</remarks>
    internal sealed class CompletableToSingle<T> : ISingleSource<T>
    {
        readonly ICompletableSource source;

        readonly T item;

        public CompletableToSingle(ICompletableSource source, T item)
        {
            this.source = source;
            this.item = item;
        }

        public void Subscribe(ISingleObserver<T> observer)
        {
            source.Subscribe(new ToSingleObserver(observer, item));
        }

        sealed class ToSingleObserver : ICompletableObserver, IDisposable
        {
            readonly ISingleObserver<T> downstream;

            readonly T item;

            IDisposable upstream;

            public ToSingleObserver(ISingleObserver<T> downstream, T item)
            {
                this.downstream = downstream;
                this.item = item;
            }

            public void Dispose()
            {
                upstream?.Dispose();
                upstream = null;
            }

            public void OnCompleted()
            {
                upstream = null;
                downstream.OnSuccess(item);
            }

            public void OnError(Exception error)
            {
                upstream = null;
                downstream.OnError(error);
            }

            public void OnSubscribe(IDisposable d)
            {
                upstream = d;
                downstream.OnSubscribe(this);
            }
        }
    }
}
