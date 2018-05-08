using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Base abstract class for implementing IObservers that talk to
    /// another IObserver and supports deferred dispose of an upstream
    /// set via OnSubscribe().
    /// </summary>
    /// <typeparam name="T">The upstream value type.</typeparam>
    /// <typeparam name="R">The downstream value type.</typeparam>
    internal abstract class BaseObserver<T, R> : IObserver<T>, IDisposable
    {
        internal readonly IObserver<R> downstream;

        internal IDisposable upstream;

        internal BaseObserver(IObserver<R> downstream)
        {
            this.downstream = downstream;
        }

        public abstract void OnCompleted();

        public abstract void OnError(Exception error);

        public abstract void OnNext(T value);

        internal virtual void OnSubscribe(IDisposable d)
        {
            if (Interlocked.CompareExchange(ref upstream, d, null) != null)
            {
                d?.Dispose();
            }
        }

        internal bool IsDisposed()
        {
            return DisposableHelper.IsDisposed(ref upstream);
        }

        public virtual void Dispose()
        {
            DisposableHelper.Dispose(ref upstream);
        }
    }
}
