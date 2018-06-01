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
    /// <typeparam name="U">The element type of the handler observable.</typeparam>
    /// <remarks>Since 0.0.10</remarks>
    internal sealed class CompletableRetryWhen<U> : ICompletableSource
    {
        readonly ICompletableSource source;

        readonly Func<IObservable<Exception>, IObservable<U>> handler;

        internal CompletableRetryWhen(ICompletableSource source, Func<IObservable<Exception>, IObservable<U>> handler)
        {
            this.source = source;
            this.handler = handler;
        }

        public void Subscribe(ICompletableObserver observer)
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
                DisposableHelper.Error(observer, ex);
                return;
            }

            var parent = new MainObserver(observer, source, new SerializedObserver<Exception>(errorSignals));

            observer.OnSubscribe(parent);

            var d = redo.Subscribe(parent.handlerObserver);
            parent.handlerObserver.OnSubscribe(d);

            parent.HandlerNext();
        }

        sealed class MainObserver : CompletableRedoWhenObserver<U, Exception>
        {

            internal MainObserver(ICompletableObserver downstream, ICompletableSource source, IObserver<Exception> errorSignal) : base(downstream, source, errorSignal)
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