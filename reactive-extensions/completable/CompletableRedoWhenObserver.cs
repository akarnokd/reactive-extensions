using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// A completable observer that re-subscribes to a source
    /// when the handlerObserver receives a item.
    /// </summary>
    /// <remarks>Since 0.0.10</remarks>
    internal abstract class CompletableRedoWhenObserver<U, X> : ICompletableObserver, IDisposable
    {
        protected readonly ICompletableObserver downstream;

        protected readonly IObserver<X> terminalSignal;

        internal readonly HandlerObserver handlerObserver;

        readonly ICompletableSource source;

        int trampoline;

        Exception error;

        IDisposable upstream;

        int once;

        internal CompletableRedoWhenObserver(ICompletableObserver downstream, ICompletableSource source, IObserver<X> errorSignal)
        {
            this.downstream = downstream;
            this.source = source;
            this.terminalSignal = errorSignal;
            this.handlerObserver = new HandlerObserver(this);
        }

        internal void HandlerError(Exception error)
        {
            if (Interlocked.CompareExchange(ref this.error, error, null) == null)
            {
                downstream.OnError(error);
                Dispose();
            }
        }

        internal void HandleSignal(X signal)
        {
            Interlocked.Exchange(ref once, 0);
            terminalSignal.OnNext(signal);
        }

        internal void HandlerComplete()
        {
            var error = ExceptionHelper.TERMINATED;
            if (Interlocked.CompareExchange(ref this.error, error, null) == null)
            {
                downstream.OnCompleted();
                Dispose();
            }
        }

        internal void HandlerNext()
        {
            if (Interlocked.Increment(ref trampoline) == 1)
            {
                do
                {
                    if (Interlocked.CompareExchange(ref once, 1, 0) == 0)
                    {
                        source.Subscribe(this);
                    }
                }
                while (Interlocked.Decrement(ref trampoline) != 0);
            }
        }

        public virtual void Dispose()
        {
            DisposableHelper.Dispose(ref upstream);
            handlerObserver.Dispose();
        }

        public void OnSubscribe(IDisposable d)
        {
            DisposableHelper.Replace(ref upstream, d);
        }

        public abstract void OnError(Exception error);

        public abstract void OnCompleted();

        internal sealed class HandlerObserver : IObserver<U>, IDisposable
        {
            readonly CompletableRedoWhenObserver<U, X> main;

            IDisposable upstream;

            internal HandlerObserver(CompletableRedoWhenObserver<U, X> main)
            {
                this.main = main;
            }

            internal void OnSubscribe(IDisposable d)
            {
                DisposableHelper.SetOnce(ref upstream, d);
            }

            public void Dispose()
            {
                DisposableHelper.Dispose(ref upstream);
            }

            public void OnCompleted()
            {
                main.HandlerComplete();
                Dispose();
            }

            public void OnError(Exception error)
            {
                main.HandlerError(error);
                Dispose();
            }

            public void OnNext(U value)
            {
                main.HandlerNext();
            }
        }

    }
}
