using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace akarnokd.reactive_extensions
{
    internal sealed class ObservableSourceIgnoreElementsTask<T> : ISignalObserver<T>, IDisposable
    {
        readonly TaskCompletionSource<T> tcs;

        IDisposable upstream;

        CancellationTokenRegistration reg;

        internal Task Task { get { return tcs.Task; } }

        public ObservableSourceIgnoreElementsTask(CancellationTokenSource cts)
        {
            tcs = new TaskCompletionSource<T>();
            if (cts != null)
            {
                reg = cts.Token.Register(@this => ((IDisposable)@this).Dispose(), this);
            }
        }

        public void Dispose()
        {
            if (DisposableHelper.Dispose(ref upstream))
            {
                tcs.TrySetCanceled();
                reg.Dispose();
            }
        }

        public void OnCompleted()
        {
            tcs.TrySetResult(default(T));
            reg.Dispose();
        }

        public void OnError(Exception ex)
        {
            tcs.TrySetException(ex);
            reg.Dispose();
        }

        public void OnNext(T item)
        {
            // deliberately ignored
        }

        public void OnSubscribe(IDisposable d)
        {
            DisposableHelper.SetOnce(ref upstream, d);
        }
    }
}
