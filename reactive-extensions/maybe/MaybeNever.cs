using System;
using System.Collections.Generic;
using System.Text;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Never terminates.
    /// </summary>
    internal sealed class MaybeNever<T> : IMaybeSource<T>
    {
        internal static readonly IMaybeSource<T> INSTANCE = new MaybeNever<T>();

        public void Subscribe(IMaybeObserver<T> observer)
        {
            observer.OnSubscribe(DisposableHelper.EMPTY);
        }
    }
}
