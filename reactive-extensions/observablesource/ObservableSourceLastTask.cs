using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace akarnokd.reactive_extensions
{
    internal sealed class ObservableSourceLastTask<T> : ISignalObserver<T>, IDisposable
    {
        readonly TaskCompletionSource<T> tcs;

        IDisposable upstream;

        CancellationTokenRegistration reg;

        T last;
        bool hasLast;

        internal Task<T> Task { get { return tcs.Task; } }

        public ObservableSourceLastTask(CancellationTokenSource cts)
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
            if (hasLast)
            {
                reg.Dispose();
                tcs.TrySetResult(last);
            }
            else
            {
                reg.Dispose();
                tcs.TrySetException(new IndexOutOfRangeException());
            }
        }

        public void OnError(Exception ex)
        {
            reg.Dispose();
            tcs.TrySetException(ex);
        }

        public void OnNext(T item)
        {
            hasLast = true;
            last = item;
        }

        public void OnSubscribe(IDisposable d)
        {
            DisposableHelper.SetOnce(ref upstream, d);
        }
    }
}
