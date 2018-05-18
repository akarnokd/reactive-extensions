using System;
using System.Collections.Generic;
using System.Text;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Succeed with an item.
    /// </summary>
    /// <typeparam name="T">The type of the item.</typeparam>
    /// <remarks>Since 0.0.9</remarks>
    internal sealed class MaybeJust<T> : IMaybeSource<T>
    {
        readonly T item;

        public MaybeJust(T item)
        {
            this.item = item;
        }

        public void Subscribe(IMaybeObserver<T> observer)
        {
            observer.OnSubscribe(DisposableHelper.EMPTY);
            observer.OnSuccess(item);
        }
    }
}
