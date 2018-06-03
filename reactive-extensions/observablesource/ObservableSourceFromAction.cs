using System;
using System.Collections.Generic;
using System.Text;

namespace akarnokd.reactive_extensions
{
    internal sealed class ObservableSourceFromAction<T> : IObservableSource<T>, IDynamicValue<T>
    {
        readonly Action action;

        public ObservableSourceFromAction(Action action)
        {
            this.action = action;
        }

        public T GetValue(out bool success)
        {
            action();
            success = false;
            return default(T);
        }

        public void Subscribe(ISignalObserver<T> observer)
        {
            var parent = new FromActionDisposable(observer);
            observer.OnSubscribe(parent);

            if (parent.IsDisposed())
            {
                return;
            }

            try
            {
                action();
            }
            catch (Exception ex)
            {
                if (!parent.IsDisposed())
                {
                    parent.Error(ex);
                }
                return;
            }

            parent.Complete();
        }

        sealed class FromActionDisposable : DeferredScalarDisposable<T>
        {
            internal FromActionDisposable(ISignalObserver<T> downstream) : base(downstream)
            {
            }
        }
    }
}
