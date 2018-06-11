using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using static akarnokd.reactive_extensions.ValidationHelper;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Retry a failed upstream when a secondary sequence signals an
    /// item in response to that failure.
    /// </summary>
    /// <typeparam name="T">The element type of the main observable.</typeparam>
    /// <typeparam name="U">The element type of the handler observable.</typeparam>
    /// <remarks>Since 0.0.4</remarks>
    internal sealed class RetryWhen<T, U> : IObservable<T>
    {
        readonly IObservable<T> source;

        readonly Func<IObservable<Exception>, IObservable<U>> handler;

        internal RetryWhen(IObservable<T> source, Func<IObservable<Exception>, IObservable<U>> handler)
        {
            this.source = source;
            this.handler = handler;
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            RequireNonNull(observer, nameof(observer));

            var errorSignals = new UnicastSubject<Exception>();
            var redo = default(IObservable<U>);

            try
            {
                redo = RequireNonNullRef(handler(errorSignals), "The handler returned a null IObservable");
            }
            catch (Exception ex)
            {
                observer.OnError(ex);
                return DisposableHelper.EMPTY;
            }

            var parent = new MainObserver(observer, source, new SerializedObserver<Exception>(errorSignals));

            var d = redo.Subscribe(parent.handlerObserver);
            parent.handlerObserver.OnSubscribe(d);

            parent.HandlerNext();

            return parent;
        }

        sealed class MainObserver : RedoWhenObserver<T, U, Exception>
        {

            internal MainObserver(IObserver<T> downstream, IObservable<T> source, IObserver<Exception> errorSignal) : base(downstream, source, errorSignal)
            {
            }

            public override void OnCompleted()
            {
                HandlerComplete();
            }

            public override void OnError(Exception error)
            {
                HandleSignal(error);
            }
        }
    }
}