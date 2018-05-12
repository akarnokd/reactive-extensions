using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    internal abstract class RedoWhenObserver<T, U, X> : BaseObserver<T, T>
    {
        protected readonly IObserver<X> terminalSignal;

        internal readonly HandlerObserver handlerObserver;

        readonly IObservable<T> source;

        int trampoline;

        protected int halfSerializer;

        Exception error;

        internal RedoWhenObserver(IObserver<T> downstream, IObservable<T> source, IObserver<X> errorSignal) : base(downstream)
        {
            this.source = source;
            this.terminalSignal = errorSignal;
            this.handlerObserver = new HandlerObserver(this);
        }

        internal void HandlerError(Exception error)
        {
            if (Interlocked.CompareExchange(ref this.error, error, null) == null)
            {
                if (Interlocked.Increment(ref halfSerializer) == 1)
                {
                    downstream.OnError(error);
                    Dispose();
                }
            }
        }

        internal void HandleSignal(X signal)
        {
            for (; ; )
            {
                var d = Volatile.Read(ref upstream);
                if (d == DisposableHelper.DISPOSED)
                {
                    break;
                }
                if (Interlocked.CompareExchange(ref upstream, null, d) == d)
                {
                    d.Dispose();
                    terminalSignal.OnNext(signal);
                    break;
                }
            }
        }

        internal void HandlerComplete()
        {
            if (Interlocked.Increment(ref halfSerializer) == 1)
            {
                var ex = ExceptionHelper.Terminate(ref error);
                if (ex == null)
                {
                    downstream.OnCompleted();
                }
                else
                {
                    downstream.OnError(ex);
                }
                Dispose();
            }
        }

        internal void HandlerNext()
        {
            if (Interlocked.Increment(ref trampoline) == 1)
            {
                do
                {
                    var sad = new SingleAssignmentDisposable();
                    if (Interlocked.CompareExchange(ref upstream, sad, null) != null)
                    {
                        return;
                    }

                    sad.Disposable = source.Subscribe(this);
                }
                while (Interlocked.Decrement(ref trampoline) != 0);
            }
        }

        public override void Dispose()
        {
            DisposableHelper.Dispose(ref upstream);
            handlerObserver.Dispose();
        }

        public override void OnNext(T value)
        {
            if (Interlocked.CompareExchange(ref halfSerializer, 1, 0) == 0)
            {
                downstream.OnNext(value);
                if (Interlocked.Decrement(ref halfSerializer) != 0)
                {
                    var ex = error;
                    if (ex == null)
                    {
                        downstream.OnCompleted();
                    }
                    else
                    {
                        downstream.OnError(ex);
                    }
                    Dispose();
                }
            }
        }


        internal sealed class HandlerObserver : IObserver<U>, IDisposable
        {
            readonly RedoWhenObserver<T, U, X> main;

            IDisposable upstream;

            internal HandlerObserver(RedoWhenObserver<T, U, X> main)
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
