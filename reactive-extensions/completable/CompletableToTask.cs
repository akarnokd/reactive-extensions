using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Subscribe to a completable source and expose the terminal
    /// signal as a <see cref="Task"/>.
    /// </summary>
    /// <remarks>Since 0.0.9</remarks>
    internal sealed class CompletableToTask : ICompletableObserver
    {
        readonly TaskCompletionSource<object> tcs;

        public Task Task { get { return tcs.Task; } }

        IDisposable upstream;

        CancellationTokenRegistration reg;

        bool hasTokenSource;

        public CompletableToTask()
        {
            tcs = new TaskCompletionSource<object>();
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
            tcs.TrySetResult(null);
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
