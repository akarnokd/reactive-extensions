using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    internal sealed class ObservableSourceError<T> : IObservableSource<T>, IDynamicValue<T>
    {
        readonly Exception error;

        public ObservableSourceError(Exception error)
        {
            this.error = error; ;
        }

        public T GetValue(out bool success)
        {
            throw error;
        }

        public void Subscribe(ISignalObserver<T> observer)
        {
            DisposableHelper.Error(observer, error);
        }
    }
}
