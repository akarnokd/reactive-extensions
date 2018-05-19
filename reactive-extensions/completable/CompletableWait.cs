using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{

    /// <summary>
    /// Wait until the upstream terminates and rethrow any exception.
    /// </summary>
    /// <remarks>Since 0.0.10</remarks>
    internal sealed class CompletableWait : CountdownEvent, ICompletableObserver
    {
        IDisposable upstream;

        Exception error;

        public CompletableWait() : base(1)
        {
        }

        public void OnCompleted()
        {
            Signal();
        }

        public void OnError(Exception error)
        {
            this.error = error;
            Signal();
        }

        public void OnSubscribe(IDisposable d)
        {
            DisposableHelper.SetOnce(ref upstream, d);
        }

        void DisposeUpstream()
        {
            DisposableHelper.Dispose(ref upstream);
        }

        public void Wait(int timeout, CancellationTokenSource cts)
        {
            if (CurrentCount != 0)
            {
                if (cts != null)
                {
                    if (timeout == int.MaxValue)
                    {
                        try
                        {
                            base.Wait(cts.Token);
                        }
                        catch
                        {
                            DisposeUpstream();
                            throw;
                        }
                    }
                    else
                    {
                        try
                        {
                            if (!base.Wait(timeout, cts.Token))
                            {
                                throw new TimeoutException();
                            }
                        }
                        catch
                        {
                            DisposeUpstream();
                            throw;
                        }
                    }
                }
                else
                {
                    if (timeout == int.MaxValue)
                    {
                        try
                        {
                            base.Wait();
                        }
                        catch
                        {
                            DisposeUpstream();
                            throw;
                        }
                    }
                    else
                    {
                        try
                        {
                            if (!base.Wait(timeout))
                            {
                                throw new TimeoutException();
                            }
                        }
                        catch
                        {
                            DisposeUpstream();
                            throw;
                        }
                    }
                }
            }
            var ex = error;
            if (ex != null)
            {
                throw ex;
            }
        }
    }
}
