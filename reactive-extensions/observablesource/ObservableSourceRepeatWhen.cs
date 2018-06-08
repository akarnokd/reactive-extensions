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
    internal sealed class ObservableSourceRepeatWhen<T, U> : IObservableSource<T>
    {
        readonly IObservableSource<T> source;

        readonly Func<IObservableSource<object>, IObservableSource<U>> handler;

        internal ObservableSourceRepeatWhen(IObservableSource<T> source, Func<IObservableSource<object>, IObservableSource<U>> handler)
        {
            this.source = source;
            this.handler = handler;
        }

        public void Subscribe(ISignalObserver<T> observer)
        {
            var completeSignals = new PublishSubject<object>();
            var redo = default(IObservableSource<U>);

            try
            {
                redo = ValidationHelper.RequireNonNullRef(handler(completeSignals), "The handler returned a null IObservableSource");
            }
            catch (Exception ex)
            {
                DisposableHelper.Error(observer, ex);
                return;
            }

            var parent = new MainObserver(observer, source, new SerializedSignalObserver<object>(completeSignals));
            observer.OnSubscribe(parent);

            redo.Subscribe(parent.handlerObserver);

            parent.HandlerNext();
        }

        sealed class MainObserver : RedoWhenSignalObserver<T, U, object>
        {

            internal MainObserver(ISignalObserver<T> downstream, IObservableSource<T> source, ISignalObserver<object> errorSignal) : base(downstream, source, errorSignal)
            {
            }

            public override void OnCompleted()
            {
                HandleSignal(null);
            }

            public override void OnError(Exception error)
            {
                MainError(error);
            }
        }
    }
}