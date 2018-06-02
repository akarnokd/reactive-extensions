using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using static akarnokd.reactive_extensions.ValidationHelper;

namespace akarnokd.reactive_extensions
{
    internal sealed class ObservableSourceDefer<T> : IObservableSource<T> {
        readonly Func<IObservableSource<T>> supplier;

        public ObservableSourceDefer(Func<IObservableSource<T>> supplier)
        {
            this.supplier = supplier;
        }

        public void Subscribe(ISignalObserver<T> observer)
        {
            var source = default(IObservableSource<T>);

            try
            {
                source = RequireNonNullRef(supplier(), "The supplier returned a null IObservableSource");
            }
            catch (Exception ex)
            {
                DisposableHelper.Error(observer, ex);
                return;
            }

            source.Subscribe(observer);
        }
    }
}
