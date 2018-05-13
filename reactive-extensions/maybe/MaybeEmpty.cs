using System;
using System.Collections.Generic;
using System.Text;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Completes immediately.
    /// </summary>
    internal sealed class MaybeEmpty<T> : IMaybeSource<T>
    {
        internal static readonly IMaybeSource<T> INSTANCE = new MaybeEmpty<T>();

        public void Subscribe(IMaybeObserver<T> observer)
        {
            observer.Complete();
        }
    }
}
