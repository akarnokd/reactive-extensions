using System;
using System.Collections.Generic;
using System.Text;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Wraps and calls the given action for each individual
    /// maybe observer then completes or fails the observer
    /// depending on the action completes normally or threw an exception.
    /// </summary>
    /// <remarks>Since 0.0.11</remarks>
    internal sealed class MaybeFromAction<T> : IMaybeSource<T>
    {
        readonly Action action;

        public MaybeFromAction(Action action)
        {
            this.action = action;
        }

        public void Subscribe(IMaybeObserver<T> observer)
        {
            var bd = new BooleanDisposable();
            observer.OnSubscribe(bd);

            if (bd.IsDisposed())
            {
                return;
            }

            try
            {
                action();
            }
            catch (Exception ex)
            {
                if (!bd.IsDisposed())
                {
                    observer.OnError(ex);
                }
                return;
            }

            if (!bd.IsDisposed())
            {
                observer.OnCompleted();
            }
        }
    }
}
