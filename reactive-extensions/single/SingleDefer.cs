using System;
using System.Collections.Generic;
using System.Text;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Defers the creation of the actual single source
    /// provided by a supplier function until a single observer completes.
    /// </summary>
    /// <typeparam name="T">The success value type</typeparam>
    /// <remarks>Since 0.0.11</remarks>
    internal sealed class SingleDefer<T> : ISingleSource<T>
    {
        readonly Func<ISingleSource<T>> supplier;

        public SingleDefer(Func<ISingleSource<T>> supplier)
        {
            this.supplier = supplier;
        }

        public void Subscribe(ISingleObserver<T> observer)
        {
            var c = default(ISingleSource<T>);
            try
            {
                c = ValidationHelper.RequireNonNullRef(supplier(), "The supplier returned a null ISingleSource");
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
