using System;
using System.Collections.Generic;
using System.Text;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Never terminates.
    /// </summary>
    internal sealed class CompletableNever : ICompletableSource
    {
        internal static readonly ICompletableSource INSTANCE = new CompletableNever();

        public void Subscribe(ICompletableObserver observer)
        {
            observer.OnSubscribe(DisposableHelper.EMPTY);
        }
    }
}
