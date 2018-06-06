using System;
using System.Collections.Generic;
using System.Text;

namespace akarnokd.reactive_extensions
{
    internal sealed class LambdaSignalObserver<T> : ISignalObserver<T>, IDisposable
    {
        readonly Action<IDisposable> onSubscribe;

        readonly Action<T> onNext;

        readonly Action<Exception> onError;

        readonly Action onCompleted;

        IDisposable upstream;

        public LambdaSignalObserver(Action<IDisposable> onSubscribe, Action<T> onNext, Action<Exception> onError, Action onCompleted)
        {
            this.onSubscribe = onSubscribe;
            this.onNext = onNext;
            this.onError = onError;
            this.onCompleted = onCompleted;
        }

        public void Dispose()
        {
            DisposableHelper.Dispose(ref upstream);
        }

        public void OnCompleted()
        {
            if (!DisposableHelper.IsDisposed(ref upstream))
            {
                DisposableHelper.WeakDispose(ref upstream);
                try
                {
                    onCompleted?.Invoke();
                }
                catch (Exception)
                {
                    // TODO where to put these?
                }
            }
        }

        public void OnError(Exception ex)
        {
            if (!DisposableHelper.IsDisposed(ref upstream))
            {
                DisposableHelper.WeakDispose(ref upstream);
                try
                {
                    onError?.Invoke(ex);
                }
                catch (Exception)
                {
                    // TODO where to put these?
                }
            }
        }

        public void OnNext(T item)
        {
            if (!DisposableHelper.IsDisposed(ref upstream))
            {

                try
                {
                    onNext?.Invoke(item);
                }
                catch (Exception ex)
                {
                    upstream.Dispose();
                    OnError(ex);
                }
            }
        }

        public void OnSubscribe(IDisposable d)
        {
            DisposableHelper.SetOnce(ref upstream, d);
            try
            {
                onSubscribe?.Invoke(this);
            }
            catch (Exception ex)
            {
                d.Dispose();
                OnError(ex);
            }
        }
    }
}
