using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Blocks until the upstream completes and then calls
    /// the OnSuccess/OnError/OnCompleted on the downstream.
    /// </summary>
    /// <typeparam name="T">The success value type.</typeparam>
    /// <remarks>Since 0.0.11</remarks>
    internal sealed class SingleBlockingObserver<T> : CountdownEvent, ISingleObserver<T>, IDisposable
    {
        readonly ISingleObserver<T> downstream;

        IDisposable upstream;

        Exception error;

        int once;

        T value;

        internal SingleBlockingObserver(ISingleObserver<T> downstream) : base(1)
        {
            this.downstream = downstream;
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

        public new void Dispose()
        {
            if (DisposableHelper.Dispose(ref upstream))
            {
                Unblock();
            }
        }

        public void OnSuccess(T item)
        {
            DisposableHelper.WeakDispose(ref upstream);
            value = item;
            error = ExceptionHelper.TERMINATED;
            Unblock();
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
                downstream.OnError(ex);
            }
            else
            {
                downstream.OnSuccess(value);
            }
        }
    }
}
