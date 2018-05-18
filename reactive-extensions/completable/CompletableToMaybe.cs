using System;
using System.Collections.Generic;
using System.Text;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// When the upstream completable source completes, the
    /// downstream maybe observer receives a success item.
    /// </summary>
    /// <typeparam name="T">The type of the success item.</typeparam>
    /// <remarks>Since 0.0.9</remarks>
    internal sealed class CompletableToMaybeSuccess<T> : IMaybeSource<T>
    {
        readonly ICompletableSource source;

        readonly T item;

        public CompletableToMaybeSuccess(ICompletableSource source, T item)
        {
            this.source = source;
            this.item = item;
        }

        public void Subscribe(IMaybeObserver<T> observer)
        {
            source.Subscribe(new ToMaybeObserver(observer, item));
        }

        sealed class ToMaybeObserver : ICompletableObserver, IDisposable
        {
            readonly IMaybeObserver<T> downstream;

            readonly T item;

            IDisposable upstream;

            public ToMaybeObserver(IMaybeObserver<T> downstream, T item)
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

    /// <summary>
    /// When the upstream completable source completes, the
    /// downstream maybe observer is completed as well.
    /// </summary>
    /// <typeparam name="T">The type of the success item.</typeparam>
    /// <remarks>Since 0.0.9</remarks>
    internal sealed class CompletableToMaybeComplete<T> : IMaybeSource<T>
    {
        readonly ICompletableSource source;

        public CompletableToMaybeComplete(ICompletableSource source)
        {
            this.source = source;
        }

        public void Subscribe(IMaybeObserver<T> observer)
        {
            source.Subscribe(new ToMaybeObserver(observer));
        }

        sealed class ToMaybeObserver : ICompletableObserver, IDisposable
        {
            readonly IMaybeObserver<T> downstream;

            IDisposable upstream;

            public ToMaybeObserver(IMaybeObserver<T> downstream)
            {
                this.downstream = downstream;
            }

            public void Dispose()
            {
                upstream?.Dispose();
                upstream = null;
            }

            public void OnCompleted()
            {
                upstream = null;
                downstream.OnCompleted();
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
