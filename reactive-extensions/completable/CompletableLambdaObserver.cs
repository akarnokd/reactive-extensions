using System;
using System.Collections.Generic;
using System.Text;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Calls the specific action callbacks upon receiving the terminal signals.
    /// </summary>
    /// <remarks>Since 0.0.6</remarks>
    internal sealed class CompletableLambdaObserver : ICompletableObserver, IDisposable
    {
        readonly Action onCompleted;

        readonly Action<Exception> onError;

        IDisposable upstream;

        public CompletableLambdaObserver(Action onCompleted, Action<Exception> onError)
        {
            this.onCompleted = onCompleted;
            this.onError = onError;
        }

        public void Dispose()
        {
            DisposableHelper.Dispose(ref upstream);
        }

        public void OnCompleted()
        {
            DisposableHelper.WeakDispose(ref upstream);
            try
            {
                onCompleted?.Invoke();
            }
            catch (Exception)
            {
                // FIXME nowhere to put these
            }
        }

        public void OnError(Exception error)
        {
            DisposableHelper.WeakDispose(ref upstream);
            try
            {
                onError?.Invoke(error);
            }
            catch (Exception)
            {
                // FIXME nowhere to put these
            }
        }

        public void OnSubscribe(IDisposable d)
        {
            DisposableHelper.SetOnce(ref upstream, d);
        }
    }
}
