using System;
using System.Collections.Generic;
using System.Text;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Suppresses an upstream error and completes the maybe observer
    /// instead.
    /// </summary>
    /// <typeparam name="T">The success value type.</typeparam>
    /// <remarks>Since 0.0.11</remarks>
    internal sealed class MaybeOnErrorComplete<T> : IMaybeSource<T>
    {
        readonly IMaybeSource<T> source;

        public MaybeOnErrorComplete(IMaybeSource<T> source)
        {
            this.source = source;
        }

        public void Subscribe(IMaybeObserver<T> observer)
        {
            source.Subscribe(new OnErrorCompleteObserver(observer));
        }

        sealed class OnErrorCompleteObserver : IMaybeObserver<T>, IDisposable
        {
            readonly IMaybeObserver<T> downstream;

            IDisposable upstream;

            public OnErrorCompleteObserver(IMaybeObserver<T> downstream)
            {
                this.downstream = downstream;
            }

            public void Dispose()
            {
                upstream.Dispose();
            }

            public void OnCompleted()
            {
                downstream.OnCompleted();
            }

            public void OnError(Exception error)
            {
                downstream.OnCompleted();
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
