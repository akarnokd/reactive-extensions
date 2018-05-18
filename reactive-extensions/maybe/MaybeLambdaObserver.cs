using System;
using System.Collections.Generic;
using System.Text;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Calls the specific action callbacks upon receiving the terminal signals.
    /// </summary>
    /// <remarks>Since 0.0.9</remarks>
    internal sealed class MaybeLambdaObserver<T> : IMaybeObserver<T>, IDisposable
    {
        readonly Action<T> onSuccess;

        readonly Action<Exception> onError;

        readonly Action onCompleted;

        IDisposable upstream;

        public MaybeLambdaObserver(Action<T> onSuccess, Action<Exception> onError, Action onCompleted)
        {
            this.onSuccess = onSuccess;
            this.onError = onError;
            this.onCompleted = onCompleted;
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
