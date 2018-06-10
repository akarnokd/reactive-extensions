using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    internal abstract class ObservableSourceBaseBlockingConsumer<T> : CountdownEvent, ISignalObserver<T>
    {
        int once;

        protected IDisposable upstream;

        protected T value;
        protected bool hasValue;
        protected Exception error;

        public ObservableSourceBaseBlockingConsumer() : base(1)
        {
        }

        public abstract void OnCompleted();
        public abstract void OnNext(T item);

        public void OnSubscribe(IDisposable d)
        {
            DisposableHelper.SetOnce(ref upstream, d);
        }

        protected void Unblock()
        {
            if (Interlocked.CompareExchange(ref once, 1, 0) == 0)
            {
                Signal();
            }
        }

        public virtual void OnError(Exception ex)
        {
            if (IsDisposedUpstream())
            {
                return;
            }
            value = default;
            error = ex;
            Unblock();
        }

        protected bool IsDisposedUpstream()
        {
            return DisposableHelper.IsDisposed(ref upstream);
        }

        public void DisposeUpstream()
        {
            DisposableHelper.Dispose(ref upstream);
        }

        internal T GetValue(out bool success, TimeSpan timeout, CancellationTokenSource cts)
        {
            if (CurrentCount != 0)
            {
                try
                {
                    if (timeout == TimeSpan.MaxValue)
                    {
                        if (cts != null)
                        {
                            Wait(cts.Token);
                        }
                        else
                        {
                            Wait();
                        }
                    }
                    else
                    {
                        if (cts != null)
                        {

                            if (!Wait(timeout, cts.Token))
                            {
                                throw new TimeoutException();
                            }
                        }
                        else
                        {
                            if (!Wait(timeout))
                            {
                                throw new TimeoutException();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    DisposeUpstream();
                    throw ex;
                }
            }

            var exc = error;
            if (exc != null)
            {
                throw exc;
            }
            if (hasValue)
            {
                success = true;
                return value;
            }
            success = false;
            return default;
        }
    }

    internal sealed class ObservableSourceBlockingFirst<T> : ObservableSourceBaseBlockingConsumer<T>
    {

        public override void OnCompleted()
        {
            Unblock();
        }

        public override void OnNext(T item)
        {
            if  (IsDisposedUpstream())
            {
                return;
            }
            value = item;
            hasValue = true;
            DisposeUpstream();
            Unblock();
        }
    }

    internal sealed class ObservableSourceBlockingLast<T> : ObservableSourceBaseBlockingConsumer<T>
    {

        public override void OnCompleted()
        {
            Unblock();
        }

        public override void OnNext(T item)
        {
            value = item;
            hasValue = true;
        }
    }

    internal sealed class ObservableSourceBlockingSingle<T> : ObservableSourceBaseBlockingConsumer<T>
    {

        public override void OnCompleted()
        {
            Unblock();
        }

        public override void OnNext(T item)
        {
            if (IsDisposedUpstream())
            {
                return;
            }
            if (hasValue)
            {
                DisposeUpstream();
                value = default;
                error = new IndexOutOfRangeException();
                Unblock();
            }
            else
            {
                value = item;
                hasValue = true;
            }
        }
    }
}
