using System;
using System.Collections.Generic;
using System.Text;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Wraps and runs a function for each incoming
    /// single observer and signals the value returned
    /// by the function as the success event.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <remarks>Since 0.0.11</remarks>
    internal sealed class SingleFromFunc<T> : ISingleSource<T>
    {
        readonly Func<T> func;

        public SingleFromFunc(Func<T> func)
        {
            this.func = func;
        }

        public void Subscribe(ISingleObserver<T> observer)
        {
            var d = new BooleanDisposable();
            observer.OnSubscribe(d);

            if (d.IsDisposed())
            {
                return;
            }

            var v = default(T);

            try
            {
                v = func();
            }
            catch (Exception ex)
            {
                if (!d.IsDisposed())
                {
                    observer.OnError(ex);
                }
                return;
            }

            if (!d.IsDisposed())
            {
                observer.OnSuccess(v);
            }
        }
    }
}
