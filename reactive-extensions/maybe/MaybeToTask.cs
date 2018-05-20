using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Subscribe to a maybe source and expose the terminal
    /// signal as a <see cref="Task{T}"/>.
    /// </summary>
    /// <typeparam name="T">The success value type.</typeparam>
    /// <remarks>Since 0.0.11</remarks>
    internal sealed class MaybeToTask<T> : IMaybeObserver<T>
    {
        readonly TaskCompletionSource<T> tcs;

        public Task<T> Task { get { return tcs.Task; } }

        IDisposable upstream;

        CancellationTokenRegistration reg;

        bool hasTokenSource;

        public MaybeToTask()
        {
            tcs = new TaskCompletionSource<T>();
        }

        internal void Init(CancellationTokenSource cts)
        {
            if (cts != null)
            {
                reg = cts.Token.Register(Dispose);
                hasTokenSource = true;
            }
        }

        public void OnCompleted()
        {
            tcs.TrySetResult(default(T));
            if (hasTokenSource)
            {
                reg.Dispose();
            }
        }

        public void OnError(Exception error)
        {
            tcs.TrySetException(error);
            if (hasTokenSource)
            {
                reg.Dispose();
            }
        }

        public void OnSuccess(T item)
        {
            tcs.TrySetResult(item);
            if (hasTokenSource)
            {
                reg.Dispose();
            }
        }

        public void OnSubscribe(IDisposable d)
        {
            DisposableHelper.SetOnce(ref upstream, d);
        }

        void Dispose()
        {
            DisposableHelper.Dispose(ref upstream);
            tcs.TrySetCanceled();
        }
    }
}
