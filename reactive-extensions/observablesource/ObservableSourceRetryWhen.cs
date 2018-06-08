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
    /// <remarks>Since 0.0.21</remarks>
    internal sealed class ObservableSourceRetryWhen<T, U> : IObservableSource<T>
    {
        readonly IObservableSource<T> source;

        readonly Func<IObservableSource<Exception>, IObservableSource<U>> handler;

        internal ObservableSourceRetryWhen(IObservableSource<T> source, Func<IObservableSource<Exception>, IObservableSource<U>> handler)
        {
            this.source = source;
            this.handler = handler;
        }

        public void Subscribe(ISignalObserver<T> observer)
        {
            if (observer == null)
            {
                throw new ArgumentNullException(nameof(observer));
            }

            var errorSignals = new PublishSubject<Exception>();
            var redo = default(IObservableSource<U>);

            try
            {
                redo = ValidationHelper.RequireNonNullRef(handler(errorSignals), "The handler returned a null IObservableSource");
            }
            catch (Exception ex)
            {
                DisposableHelper.Error(observer, ex);
                return;
            }

            var parent = new MainObserver(observer, source, new SerializedSignalObserver<Exception>(errorSignals));
            observer.OnSubscribe(parent);

            redo.Subscribe(parent.handlerObserver);

            parent.HandlerNext();
        }

        sealed class MainObserver : RedoWhenSignalObserver<T, U, Exception>
        {

            internal MainObserver(ISignalObserver<T> downstream, IObservableSource<T> source, ISignalObserver<Exception> errorSignal) : base(downstream, source, errorSignal)
            {
            }

            public override void OnCompleted()
            {
                MainComplete();
            }

            public override void OnError(Exception error)
            {
                HandleSignal(error);
            }
        }
    }
}