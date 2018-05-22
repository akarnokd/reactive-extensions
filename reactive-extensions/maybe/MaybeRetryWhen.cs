using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using static akarnokd.reactive_extensions.ValidationHelper;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Retries (resubscribes to) the maybe source after a failure and when the observable
    /// returned by a handler produces an arbitrary item.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the source sequence.</typeparam>
    /// <typeparam name="U">The arbitrary element type signaled by the handler observable.</typeparam>
    /// <remarks>Since 0.0.13</remarks>
    internal sealed class MaybeRetryWhen<T, U> : IMaybeSource<T>
    {
        readonly IMaybeSource<T> source;

        readonly Func<IObservable<Exception>, IObservable<U>> handler;

        public MaybeRetryWhen(IMaybeSource<T> source, Func<IObservable<Exception>, IObservable<U>> handler)
        {
            this.source = source;
            this.handler = handler;
        }

        public void Subscribe(IMaybeObserver<T> observer)
        {
            var terminalSignal = new UnicastSubject<Exception>();

            var redoSignal = default(IObservable<U>);

            try
            {
                redoSignal = RequireNonNullRef(handler(terminalSignal), "The handler returned a null IObservable");
            }
            catch (Exception ex)
            {
                observer.Error(ex);
                return;
            }

            var parent = new RepeatWhenObserver(observer, source, ((IObserver<Exception>)terminalSignal).ToSerialized());

            observer.OnSubscribe(parent);

            parent.redoObserver.OnSubscribe(redoSignal.Subscribe(parent.redoObserver));

            parent.Next();
        }

        internal sealed class RepeatWhenObserver : MaybeRedoWhenObserver<T, U, Exception>
        {
            readonly IMaybeObserver<T> downstream;

            public RepeatWhenObserver(IMaybeObserver<T> downstream, IMaybeSource<T> source, IObserver<Exception> terminalSignal) : base(source, terminalSignal)
            {
                this.downstream = downstream;
            }

            public override void OnCompleted()
            {
                DisposableHelper.WeakDispose(ref upstream);
                redoObserver.Dispose();
                if (Interlocked.CompareExchange(ref halfSerializer, 1, 0) == 0)
                {
                    downstream.OnCompleted();
                }
            }

            public override void OnError(Exception error)
            {
                active = false;
                terminalSignal.OnNext(error);
            }

            public override void OnSuccess(T item)
            {
                DisposableHelper.WeakDispose(ref upstream);
                redoObserver.Dispose();
                if (Interlocked.CompareExchange(ref halfSerializer, 1, 0) == 0)
                {
                    downstream.OnSuccess(item);
                }
            }

            internal override void RedoComplete()
            {
                Dispose();
                if (Interlocked.CompareExchange(ref halfSerializer, 1, 0) == 0)
                {
                    downstream.OnCompleted();
                }
            }

            internal override void RedoError(Exception ex)
            {
                Dispose();
                if (Interlocked.CompareExchange(ref halfSerializer, 1, 0) == 0)
                {
                    downstream.OnError(ex);
                }
            }

            internal override void RedoNext()
            {
                Next();
            }
        }
    }
}
