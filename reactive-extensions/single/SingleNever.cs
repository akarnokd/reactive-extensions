using System;
using System.Collections.Generic;
using System.Text;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Never terminates.
    /// </summary>
    internal sealed class SingleNever<T> : ISingleSource<T>
    {
        internal static readonly ISingleSource<T> INSTANCE = new SingleNever<T>();

        public void Subscribe(ISingleObserver<T> observer)
        {
            observer.OnSubscribe(DisposableHelper.EMPTY);
        }
    }
}
