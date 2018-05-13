using System;
using System.Collections.Generic;
using System.Text;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Signals an exception immediately.
    /// </summary>
    internal sealed class SingleError<T> : ISingleSource<T>
    {
        readonly Exception error;

        public SingleError(Exception error)
        {
            this.error = error;
        }

        public void Subscribe(ISingleObserver<T> observer)
        {
            observer.Error(error);
        }
    }
}
