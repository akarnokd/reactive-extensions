using System;
using System.Collections.Generic;
using System.Text;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Makes sure downstream exceptions are suppressed.
    /// </summary>
    internal sealed class CompletableSafeObserver : ICompletableObserver, IDisposable
    {
        readonly ICompletableObserver downstream;

        IDisposable upstream;

        public CompletableSafeObserver(ICompletableObserver downstream)
        {
            this.downstream = downstream;
        }

        public void Dispose()
        {
            upstream.Dispose();
        }

        public void OnCompleted()
        {
            try
            {
                downstream.OnCompleted();
            }
            catch (Exception)
            {
                // TODO what should happen with these?
            }
        }

        public void OnError(Exception error)
        {
            try
            {
                downstream.OnError(error);
            }
            catch (Exception)
            {
                // TODO what should happen with these?
            }
        }

        public void OnSubscribe(IDisposable d)
        {
            upstream = d;
            try
            {
                downstream.OnSubscribe(this);
            }
            catch (Exception)
            {
                d.Dispose();
                // TODO what should happen with these?
            }
        }
    }
}
