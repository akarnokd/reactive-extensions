using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    internal abstract class RedoWhenSignalObserver<T, U, X> : BaseSignalObserver<T, T>
    {
        protected readonly ISignalObserver<X> terminalSignal;

        internal readonly HandlerObserver handlerObserver;

        readonly IObservableSource<T> source;

        int trampoline;

        protected int halfSerializer;

        Exception error;

        internal RedoWhenSignalObserver(ISignalObserver<T> downstream, IObservableSource<T> source, ISignalObserver<X> errorSignal) : base(downstream)
        {
            this.source = source;
            this.terminalSignal = errorSignal;
            this.handlerObserver = new HandlerObserver(this);
        }

        internal void HandlerError(Exception error)
        {
            DisposableHelper.Dispose(ref upstream);
            if (Interlocked.CompareExchange(ref this.error, error, null) == null)
            {
                if (Interlocked.Increment(ref halfSerializer) == 1)
                {
                    downstream.OnError(error);
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

        internal void MainError(Exception error)
        {
            handlerObserver.Dispose();
            if (Interlocked.CompareExchange(ref this.error, error, null) == null)
            {
                if (Interlocked.Increment(ref halfSerializer) == 1)
                {
                    downstream.OnError(error);
                }
            }
        }

        internal void MainComplete()
        {
            handlerObserver.Dispose();
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
            }
        }

        internal void HandlerComplete()
        {
            DisposableHelper.Dispose(ref upstream);
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
            }
        }

        internal void HandlerNext()
        {
            if (Interlocked.Increment(ref trampoline) == 1)
            {
                do
                {
                    if (Volatile.Read(ref upstream) == null)
                    {
                        source.Subscribe(this);
                    }
                }
                while (Interlocked.Decrement(ref trampoline) != 0);
            }
        }

        public override void OnSubscribe(IDisposable d)
        {
            DisposableHelper.SetOnce(ref upstream, d);
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
                    handlerObserver.Dispose();
                }
            }
        }


        internal sealed class HandlerObserver : ISignalObserver<U>, IDisposable
        {
            readonly RedoWhenSignalObserver<T, U, X> main;

            IDisposable upstream;

            internal HandlerObserver(RedoWhenSignalObserver<T, U, X> main)
            {
                this.main = main;
            }

            public void OnSubscribe(IDisposable d)
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
            }

            public void OnError(Exception error)
            {
                main.HandlerError(error);
            }

            public void OnNext(U value)
            {
                main.HandlerNext();
            }
        }

    }
}
