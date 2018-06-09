using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace akarnokd.reactive_extensions
{
    internal sealed class ObservableSourceSingleTask<T> : ISignalObserver<T>, IDisposable
    {
        readonly TaskCompletionSource<T> tcs;

        IDisposable upstream;

        CancellationTokenRegistration reg;

        T value;
        bool hasValue;

        internal Task<T> Task { get { return tcs.Task; } }

        public ObservableSourceSingleTask(CancellationTokenSource cts)
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
            if (hasValue)
            {
                reg.Dispose();
                tcs.TrySetResult(value);
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
            if (hasValue)
            {
                reg.Dispose();
                DisposableHelper.Dispose(ref upstream);
                tcs.TrySetException(new IndexOutOfRangeException());
            }
            else
            {
                hasValue = true;
                value = item;
            }
        }

        public void OnSubscribe(IDisposable d)
        {
            DisposableHelper.SetOnce(ref upstream, d);
        }
    }
}
