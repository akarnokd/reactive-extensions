using System;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// A completable observer with a thread-safe deferred
    /// OnSubscribe and Dispose support.
    /// </summary>
    /// <remarks>Since 0.0.8</remarks>
    sealed class CompletableInnerObserver : ICompletableObserver, IDisposable
    {
        readonly ICompletableObserver downstream;

        IDisposable upstream;

        public CompletableInnerObserver(ICompletableObserver downstream)
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
    }
}
