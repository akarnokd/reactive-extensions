using System;
using System.Collections.Generic;
using System.Text;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Defers the creation of the actual maybe source
    /// provided by a supplier function until a maybe observer completes.
    /// </summary>
    /// <typeparam name="T">The success value type</typeparam>
    /// <remarks>Since 0.0.11</remarks>
    internal sealed class MaybeDefer<T> : IMaybeSource<T>
    {
        readonly Func<IMaybeSource<T>> supplier;

        public MaybeDefer(Func<IMaybeSource<T>> supplier)
        {
            this.supplier = supplier;
        }

        public void Subscribe(IMaybeObserver<T> observer)
        {
            var c = default(IMaybeSource<T>);
            try
            {
                c = ValidationHelper.RequireNonNullRef(supplier(), "The supplier returned a null IMaybeSource");
            }
            catch (Exception ex)
            {
                DisposableHelper.Error(observer, ex);
                return;
            }

            c.Subscribe(observer);
        }
    }
}
