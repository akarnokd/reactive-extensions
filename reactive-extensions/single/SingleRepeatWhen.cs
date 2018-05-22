using System;
using System.Collections.Generic;
using System.Text;
using static akarnokd.reactive_extensions.ValidationHelper;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Repeats (resubscribes to) the single after a success or completion and when the observable
    /// returned by a handler produces an arbitrary item.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the source sequence.</typeparam>
    /// <typeparam name="U">The arbitrary element type signaled by the handler observable.</typeparam>
    /// <remarks>Since 0.0.13</remarks>
    internal sealed class SingleRepeatWhen<T, U> : IObservable<T>
    {
        readonly ISingleSource<T> source;

        readonly Func<IObservable<object>, IObservable<U>> handler;

        public SingleRepeatWhen(ISingleSource<T> source, Func<IObservable<object>, IObservable<U>> handler)
        {
            this.source = source;
            this.handler = handler;
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            var terminalSignal = new UnicastSubject<object>();

            var redoSignal = default(IObservable<U>);

            try
            {
                redoSignal = RequireNonNullRef(handler(terminalSignal), "The handler returned a null IObservable");
            }
            catch (Exception ex)
            {
                observer.OnError(ex);
                return DisposableHelper.EMPTY;
            }

            var parent = new RepeatWhenObserver(observer, source, ((IObserver<object>)terminalSignal).ToSerialized());

            parent.redoObserver.OnSubscribe(redoSignal.Subscribe(parent.redoObserver));

            parent.Next();

            return parent;
        }

        internal sealed class RepeatWhenObserver : SingleRedoWhenObserver<T, U, object>
        {
            readonly IObserver<T> downstream;

            public RepeatWhenObserver(IObserver<T> downstream, ISingleSource<T> source, IObserver<object> terminalSignal) : base(source, terminalSignal)
            {
                this.downstream = downstream;
            }

            public override void OnError(Exception error)
            {
                DisposableHelper.WeakDispose(ref upstream);

                HalfSerializer.OnError(downstream, error, ref halfSerializer, ref this.error);

                redoObserver.Dispose();
            }

            public override void OnSuccess(T item)
            {
                HalfSerializer.OnNext(downstream, item, ref halfSerializer, ref this.error);
                active = false;
                terminalSignal.OnNext(null);
            }

            internal override void RedoComplete()
            {
                HalfSerializer.OnCompleted(downstream, ref halfSerializer, ref this.error);
                Dispose();
            }

            internal override void RedoError(Exception ex)
            {
                HalfSerializer.OnError(downstream, ex, ref halfSerializer, ref this.error);
                Dispose();
            }

            internal override void RedoNext()
            {
                Next();
            }
        }
    }
}
