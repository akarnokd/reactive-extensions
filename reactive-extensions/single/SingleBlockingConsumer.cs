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
    internal sealed class SingleBlockingConsumer<T> : CountdownEvent, ISingleObserver<T>, IDisposable
    {
        readonly Action<T> onSuccess;

        readonly Action<Exception> onError;

        IDisposable upstream;

        Exception error;

        int once;

        T value;

        internal SingleBlockingConsumer(Action<T> onSuccess, Action<Exception> onError) : base(1)
        {
            this.onSuccess = onSuccess;
            this.onError = onError;
        }

        void Unblock()
        {
            if (Interlocked.CompareExchange(ref once, 1, 0) == 0)
            {
                Signal();
            }
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
                onSuccess?.Invoke(value);
            }
        }
    }
}
