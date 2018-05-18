using System;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Abstract base class for repeatedly subscribing to a source
    /// upon a call to <see cref="Drain"/>.
    /// Override <see cref="OnCompleted"/> or <see cref="OnError"/>
    /// to perform this for the appropriate event.
    /// </summary>
    internal abstract class CompletableRedoObserver : ICompletableObserver, IDisposable
    {
        protected readonly ICompletableObserver downstream;

        readonly ICompletableSource source;

        IDisposable upstream;

        int wip;

        public CompletableRedoObserver(ICompletableObserver downstream, ICompletableSource source)
        {
            this.downstream = downstream;
            this.source = source;
        }

        public void Dispose()
        {
            DisposableHelper.Dispose(ref upstream);
        }

        public virtual void OnCompleted()
        {
            downstream.OnCompleted();
        }

        public virtual void OnError(Exception error)
        {
            downstream.OnError(error);
        }

        public void OnSubscribe(IDisposable d)
        {
            DisposableHelper.Replace(ref upstream, d);
        }

        internal void Drain()
        {
            if (Interlocked.Increment(ref wip) == 1)
            {
                for (; ; )
                {
                    if (!DisposableHelper.IsDisposed(ref upstream))
                    {
                        source.Subscribe(this);
                    }

                    if (Interlocked.Decrement(ref wip) == 0)
                    {
                        break;
                    }
                }
            }
        }
    }
}
