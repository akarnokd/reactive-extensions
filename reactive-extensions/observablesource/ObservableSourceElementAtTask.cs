using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace akarnokd.reactive_extensions
{
    internal sealed class ObservableSourceElementAtTask<T> : ISignalObserver<T>, IDisposable
    {
        readonly TaskCompletionSource<T> tcs;

        IDisposable upstream;

        CancellationTokenRegistration reg;

        long index;

        internal Task<T> Task { get { return tcs.Task; } }

        public ObservableSourceElementAtTask(long index, CancellationTokenSource cts)
        {
            this.index = index;
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
            if (index >= 0L)
            {
                reg.Dispose();
                tcs.TrySetException(new IndexOutOfRangeException());
            }
        }

        public void OnError(Exception ex)
        {
            if (index >= 0L)
            {
                reg.Dispose();
                tcs.TrySetException(ex);
            }
        }

        public void OnNext(T item)
        {
            var idx = index;

            if (idx == 0L)
            {
                DisposableHelper.Dispose(ref upstream);
                tcs.TrySetResult(item);
                reg.Dispose();
            }

            index = idx - 1;
        }

        public void OnSubscribe(IDisposable d)
        {
            DisposableHelper.SetOnce(ref upstream, d);
        }
    }
}
