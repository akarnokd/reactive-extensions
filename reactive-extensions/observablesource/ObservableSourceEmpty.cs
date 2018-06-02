using System;
using System.Collections.Generic;
using System.Text;

namespace akarnokd.reactive_extensions
{
    internal sealed class ObservableSourceEmpty<T> : IObservableSource<T>, IStaticValue<T>
    {
        internal static readonly IObservableSource<T> Instance = new ObservableSourceEmpty<T>();

        private ObservableSourceEmpty()
        {
            // singleton
        }

        public T GetValue(out bool success)
        {
            success = false;
            return default(T);
        }

        public void Subscribe(ISignalObserver<T> observer)
        {
            DisposableHelper.Complete(observer);
        }
    }
}
