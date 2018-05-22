using System;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Base class for supporting retrying/repeating maybe sources.
    /// </summary>
    /// <typeparam name="T">The success type of the source.</typeparam>
    /// <typeparam name="U">The signal type of the handler to trigger resubscription.</typeparam>
    /// <typeparam name="X">The signal type to trigger the handler.</typeparam>
    /// <remarks>Since 0.0.13</remarks>
    internal abstract class MaybeRedoWhenObserver<T, U, X> : IMaybeObserver<T>, IDisposable
    {
        protected readonly IMaybeSource<T> source;

        protected readonly IObserver<X> terminalSignal;

        internal readonly RedoObserver redoObserver;

        protected IDisposable upstream;

        int wip;

        protected int halfSerializer;

        protected Exception error;

        protected volatile bool active;

        public MaybeRedoWhenObserver(IMaybeSource<T> source, IObserver<X> terminalSignal)
        {
            this.source = source;
            this.terminalSignal = terminalSignal;
            this.redoObserver = new RedoObserver(this);
        }

        public void Dispose()
        {
            DisposableHelper.Dispose(ref upstream);
            redoObserver.Dispose();
        }

        public abstract void OnSuccess(T item);

        public abstract void OnError(Exception error);

        public abstract void OnCompleted();

        public void OnSubscribe(IDisposable d)
        {
            DisposableHelper.Replace(ref upstream, d);
        }

        /// <summary>
        /// Trigger the next subscription if there is no active
        /// subscription currently (<see cref="active"/> is false).
        /// </summary>
        internal void Next()
        {
            if (Interlocked.Increment(ref wip) == 1)
            {
                for (; ; )
                {
                    if (!DisposableHelper.IsDisposed(ref upstream))
                    {
                        if (!active)
                        {

                            active = true;
                            source.Subscribe(this);
                        }
                    }

                    if (Interlocked.Decrement(ref wip) == 0)
                    {
                        break;
                    }
                }
            }
        }

        internal abstract void RedoNext();

        internal abstract void RedoError(Exception ex);

        internal abstract void RedoComplete();

        internal sealed class RedoObserver : IObserver<U>, IDisposable
        {
            readonly MaybeRedoWhenObserver<T, U, X> parent;

            IDisposable upstream;

            public RedoObserver(MaybeRedoWhenObserver<T, U, X> parent)
            {
                this.parent = parent;
            }

            public void Dispose()
            {
                DisposableHelper.Dispose(ref upstream);
            }

            public void OnCompleted()
            {
                parent.RedoComplete();
            }

            public void OnError(Exception error)
            {
                parent.RedoError(error);
            }

            public void OnNext(U value)
            {
                parent.RedoNext();
            }

            public void OnSubscribe(IDisposable d)
            {
                DisposableHelper.SetOnce(ref upstream, d);
            }
        }
    }
}
