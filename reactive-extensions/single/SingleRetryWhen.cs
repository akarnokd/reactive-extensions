using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using static akarnokd.reactive_extensions.ValidationHelper;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Retries (resubscribes to) the single source after a failure and when the observable
    /// returned by a handler produces an arbitrary item.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the source sequence.</typeparam>
    /// <typeparam name="U">The arbitrary element type signaled by the handler observable.</typeparam>
    /// <remarks>Since 0.0.13</remarks>
    internal sealed class SingleRetryWhen<T, U> : ISingleSource<T>
    {
        readonly ISingleSource<T> source;

        readonly Func<IObservable<Exception>, IObservable<U>> handler;

        public SingleRetryWhen(ISingleSource<T> source, Func<IObservable<Exception>, IObservable<U>> handler)
        {
            this.source = source;
            this.handler = handler;
        }

        public void Subscribe(ISingleObserver<T> observer)
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

            var parent = new RetryWhenObserver(observer, source, ((IObserver<Exception>)terminalSignal).ToSerialized());

            observer.OnSubscribe(parent);

            parent.redoObserver.OnSubscribe(redoSignal.Subscribe(parent.redoObserver));

            parent.Next();
        }

        internal sealed class RetryWhenObserver : SingleRedoWhenObserver<T, U, Exception>
        {
            readonly ISingleObserver<T> downstream;

            public RetryWhenObserver(ISingleObserver<T> downstream, ISingleSource<T> source, IObserver<Exception> terminalSignal) : base(source, terminalSignal)
            {
                this.downstream = downstream;
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
                    downstream.OnError(new IndexOutOfRangeException("The source is empty"));
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
