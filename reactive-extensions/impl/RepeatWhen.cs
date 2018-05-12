using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Retry a failed upstream when a secondary sequence signals an
    /// item in response to that failure.
    /// </summary>
    /// <typeparam name="T">The element type of the main observable.</typeparam>
    /// <typeparam name="U">The element type of the handler observable.</typeparam>
    /// <remarks>Since 0.0.4</remarks>
    internal sealed class RepeatWhen<T, U> : IObservable<T>
    {
        readonly IObservable<T> source;

        readonly Func<IObservable<object>, IObservable<U>> handler;

        internal RepeatWhen(IObservable<T> source, Func<IObservable<object>, IObservable<U>> handler)
        {
            this.source = source;
            this.handler = handler;
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            if (observer == null)
            {
                throw new ArgumentNullException(nameof(observer));
            }

            var completeSignals = new UnicastSubject<object>();
            var redo = default(IObservable<U>);

            try
            {
                redo = handler(completeSignals);
                if (redo == null)
                {
                    throw new NullReferenceException("The handler returned a null IObservable");
                }
            }
            catch (Exception ex)
            {
                observer.OnError(ex);
                return DisposableHelper.EMPTY;
            }

            var parent = new MainObserver(observer, source, new SerializedObserver<object>(completeSignals));

            var d = redo.Subscribe(parent.handlerObserver);
            parent.handlerObserver.OnSubscribe(d);

            parent.HandlerNext();

            return parent;
        }

        sealed class MainObserver : RedoWhenObserver<T, U, object>
        {

            internal MainObserver(IObserver<T> downstream, IObservable<T> source, IObserver<object> errorSignal) : base(downstream, source, errorSignal)
            {
            }

            public override void OnCompleted()
            {
                HandleSignal(null);
            }

            public override void OnError(Exception error)
            {
                HandlerError(error);
            }
        }
    }
}