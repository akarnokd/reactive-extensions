using System;
using System.Collections.Generic;
using System.Text;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Defers the creation of the actual completable source
    /// provided by a supplier function until a completable observer completes.
    /// </summary>
    /// <remarks>Since 0.0.6</remarks>
    internal sealed class CompletableDefer : ICompletableSource
    {
        readonly Func<ICompletableSource> supplier;

        public CompletableDefer(Func<ICompletableSource> supplier)
        {
            this.supplier = supplier;
        }

        public void Subscribe(ICompletableObserver observer)
        {
            var c = default(ICompletableSource);
            try
            {
                c = ValidationHelper.RequireNonNullRef(supplier(), "The supplier returned a null ICompletableSource");
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
