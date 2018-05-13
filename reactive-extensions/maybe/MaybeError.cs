using System;
using System.Collections.Generic;
using System.Text;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Signals an exception immediately.
    /// </summary>
    internal sealed class MaybeError<T> : IMaybeSource<T>
    {
        readonly Exception error;

        public MaybeError(Exception error)
        {
            this.error = error;
        }

        public void Subscribe(IMaybeObserver<T> observer)
        {
            observer.Error(error);
        }
    }
}
