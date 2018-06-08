using System;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Base abstract class for implementing ISignalObservers that talk to
    /// another IObserver and supports deferred dispose of an upstream
    /// set via OnSubscribe().
    /// </summary>
    /// <typeparam name="T">The upstream value type.</typeparam>
    /// <typeparam name="R">The downstream value type.</typeparam>
    internal abstract class BaseSignalObserver<T, R> : ISignalObserver<T>, IDisposable
    {
        internal readonly ISignalObserver<R> downstream;

        internal IDisposable upstream;

        internal BaseSignalObserver(ISignalObserver<R> downstream)
        {
            this.downstream = downstream;
        }

        public abstract void OnCompleted();

        public abstract void OnError(Exception error);

        public abstract void OnNext(T value);

        public virtual void OnSubscribe(IDisposable d)
        {
            upstream = d;
            downstream.OnSubscribe(this);
        }

        internal bool IsDisposed()
        {
            return DisposableHelper.IsDisposed(ref upstream);
        }

        public virtual void Dispose()
        {
            upstream.Dispose();
            DisposableHelper.WeakDispose(ref upstream);
        }
    }
}
