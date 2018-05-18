using System;
using System.Collections.Generic;
using System.Text;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Calls the specific action callbacks upon receiving the terminal signals.
    /// </summary>
    /// <remarks>Since 0.0.9</remarks>
    internal sealed class SingleLambdaObserver<T> : ISingleObserver<T>, IDisposable
    {
        readonly Action<T> onSuccess;

        readonly Action<Exception> onError;

        IDisposable upstream;

        public SingleLambdaObserver(Action<T> onSuccess, Action<Exception> onError)
        {
            this.onSuccess = onSuccess;
            this.onError = onError;
        }

        public void Dispose()
        {
            DisposableHelper.Dispose(ref upstream);
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

        public void OnSuccess(T item)
        {
            DisposableHelper.WeakDispose(ref upstream);
            try
            {
                onSuccess?.Invoke(item);
            }
            catch (Exception)
            {
                // FIXME nowhere to put these
            }
        }
    }
}
