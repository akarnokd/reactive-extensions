using System;
using System.Collections.Generic;
using System.Text;

namespace akarnokd.reactive_extensions
{
    internal sealed class ObservableSourceFromFunc<T> : IObservableSource<T>, IDynamicValue<T>
    {
        readonly Func<T> supplier;

        public ObservableSourceFromFunc(Func<T> supplier)
        {
            this.supplier = supplier;
        }

        public T GetValue(out bool success)
        {
            var v = supplier();
            success = true;
            return v;
        }

        public void Subscribe(ISignalObserver<T> observer)
        {
            var parent = new FromFuncDisposable(observer);
            observer.OnSubscribe(parent);

            if (parent.IsDisposed())
            {
                return;
            }

            var v = default(T);
            try
            {
                v = supplier();
            }
            catch (Exception ex)
            {
                if (!parent.IsDisposed())
                {
                    parent.Error(ex);
                }
                return;
            }

            parent.Complete(v);
        }

        sealed class FromFuncDisposable : DeferredScalarDisposable<T>
        {
            internal FromFuncDisposable(ISignalObserver<T> downstream) : base(downstream)
            {
            }
        }
    }
}
