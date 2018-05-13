using System;
using System.Collections.Generic;
using System.Text;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Signals an exception immediately.
    /// </summary>
    internal sealed class CompletableError : ICompletableSource
    {
        readonly Exception error;

        public CompletableError(Exception error)
        {
            this.error = error;
        }

        public void Subscribe(ICompletableObserver observer)
        {
            observer.Error(error);
        }
    }
}
