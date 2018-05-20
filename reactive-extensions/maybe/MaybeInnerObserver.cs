using System;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// A maybe observer with a thread-safe deferred
    /// OnSubscribe and Dispose support.
    /// </summary>
    /// <typeparam name="T">The success value type.</typeparam>
    /// <remarks>Since 0.0.11</remarks>
    internal sealed class MaybeInnerObserver<T> : IMaybeObserver<T>, IDisposable
    {
        readonly IMaybeObserver<T> downstream;

        IDisposable upstream;

        public MaybeInnerObserver(IMaybeObserver<T> downstream)
        {
            this.downstream = downstream;
        }

        public void Dispose()
        {
            DisposableHelper.Dispose(ref upstream);
        }

        public void OnCompleted()
        {
            downstream.OnCompleted();
        }

        public void OnError(Exception error)
        {
            downstream.OnError(error);
        }

        public void OnSubscribe(IDisposable d)
        {
            DisposableHelper.SetOnce(ref upstream, d);
        }

        public void OnSuccess(T item)
        {
            downstream.OnSuccess(item);
        }
    }
}
