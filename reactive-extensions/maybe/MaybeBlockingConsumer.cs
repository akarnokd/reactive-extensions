using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Blocks until the upstream completes and then calls
    /// the OnSuccess/OnError/OnCompleted delegates.
    /// </summary>
    /// <typeparam name="T">The success value type.</typeparam>
    /// <remarks>Since 0.0.11</remarks>
    internal sealed class MaybeBlockingConsumer<T> : CountdownEvent, IMaybeObserver<T>, IDisposable
    {
        readonly Action<T> onSuccess;

        readonly Action<Exception> onError;

        readonly Action onCompleted;

        IDisposable upstream;

        Exception error;

        int once;

        T value;
        bool hasValue;

        internal MaybeBlockingConsumer(Action<T> onSuccess, Action<Exception> onError, Action onCompleted) : base(1)
        {
            this.onSuccess = onSuccess;
            this.onError = onError;
            this.onCompleted = onCompleted;
        }

        void Unblock()
        {
            if (Interlocked.CompareExchange(ref once, 1, 0) == 0)
            {
                Signal();
            }
        }

        public void OnCompleted()
        {
            DisposableHelper.WeakDispose(ref upstream);
            error = ExceptionHelper.TERMINATED;
            Unblock();
        }

        public void OnError(Exception error)
        {
            DisposableHelper.WeakDispose(ref upstream);
            this.error = error;
            Unblock();
        }

        public void OnSubscribe(IDisposable d)
        {
            DisposableHelper.SetOnce(ref upstream, d);
        }

        public void OnSuccess(T item)
        {
            DisposableHelper.WeakDispose(ref upstream);
            value = item;
            hasValue = true;
            error = ExceptionHelper.TERMINATED;
            Unblock();
        }

        public new void Dispose()
        {
            if (DisposableHelper.Dispose(ref upstream))
            {
                Unblock();
            }
        }

        internal void Run()
        {
            if (CurrentCount != 0)
            {
                try
                {
                    Wait();
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
            }
            var ex = error;
            if (ex != ExceptionHelper.TERMINATED)
            {
                onError?.Invoke(ex);
            }
            else
            {
                if (hasValue)
                {
                    onSuccess?.Invoke(value);
                }
                else
                {
                    onCompleted?.Invoke();
                }
            }
        }
    }
}
