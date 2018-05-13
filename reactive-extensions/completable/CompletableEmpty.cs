using System;
using System.Collections.Generic;
using System.Text;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Completes immediately.
    /// </summary>
    internal sealed class CompletableEmpty : ICompletableSource
    {
        internal static readonly ICompletableSource INSTANCE = new CompletableEmpty();

        public void Subscribe(ICompletableObserver observer)
        {
            observer.Complete();
        }
    }
}
