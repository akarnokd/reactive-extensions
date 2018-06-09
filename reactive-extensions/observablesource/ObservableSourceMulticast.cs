using System;
using System.Collections.Generic;
using System.Text;
using static akarnokd.reactive_extensions.ValidationHelper;

namespace akarnokd.reactive_extensions
{
    internal sealed class ObservableSourceMulticast<T, R> : IObservableSource<R>
    {
        readonly IObservableSource<T> source;

        readonly Func<IObservableSubject<T>> subjectFactory;

        readonly Func<IObservableSource<T>, IObservableSource<R>> handler;

        public ObservableSourceMulticast(IObservableSource<T> source, Func<IObservableSubject<T>> subjectFactory, Func<IObservableSource<T>, IObservableSource<R>> handler)
        {
            this.source = source;
            this.subjectFactory = subjectFactory;
            this.handler = handler;
        }

        public void Subscribe(ISignalObserver<R> observer)
        {
            var subject = default(IObservableSubject<T>);
            var observable = default(IObservableSource<R>);
            try
            {
                subject = RequireNonNullRef(subjectFactory(), "The subjectFactory returned a null IObservableSubject");
                observable = RequireNonNullRef(handler(subject), "The handler returned a null IObservableSource");
            }
            catch (Exception ex)
            {
                DisposableHelper.Error(observer, ex);
                return;
            }

            var parent = new MulticastObserver(observer, subject);
            observer.OnSubscribe(parent);

            observable.Subscribe(parent);
            source.Subscribe(parent.mainObserver);
        }

        sealed class MulticastObserver : ISignalObserver<R>, IDisposable
        {
            readonly ISignalObserver<R> downstream;

            internal readonly MainObserver mainObserver;

            IDisposable upstream;

            public MulticastObserver(ISignalObserver<R> downstream, IObservableSubject<T> subject)
            {
                this.downstream = downstream;
                this.mainObserver = new MainObserver(this, subject);
            }

            public void Dispose()
            {
                DisposableHelper.Dispose(ref upstream);
                mainObserver.Dispose();
            }

            public void OnCompleted()
            {
                mainObserver.Dispose();
                downstream.OnCompleted();
            }

            public void OnError(Exception ex)
            {
                mainObserver.Dispose();
                downstream.OnError(ex);
            }

            public void OnNext(R item)
            {
                downstream.OnNext(item);
            }

            public void OnSubscribe(IDisposable d)
            {
                DisposableHelper.SetOnce(ref upstream, d);
            }

            internal sealed class MainObserver : ISignalObserver<T>, IDisposable
            {
                readonly MulticastObserver parent;

                readonly IObservableSubject<T> subject;

                IDisposable upstream;

                public MainObserver(MulticastObserver parent, IObservableSubject<T> subject)
                {
                    this.parent = parent;
                    this.subject = subject;
                }

                public void Dispose()
                {
                    DisposableHelper.Dispose(ref upstream);
                }

                public void OnCompleted()
                {
                    DisposableHelper.WeakDispose(ref upstream);
                    subject.OnCompleted();
                }

                public void OnError(Exception ex)
                {
                    DisposableHelper.WeakDispose(ref upstream);
                    subject.OnError(ex);
                }

                public void OnNext(T item)
                {
                    subject.OnNext(item);
                }

                public void OnSubscribe(IDisposable d)
                {
                    DisposableHelper.SetOnce(ref upstream, d);
                }
            }
        }
    }

    internal sealed class ObservableSourceMulticastConnect<T, U, R> : IObservableSource<R>
    {
        readonly IObservableSource<T> source;

        readonly Func<IObservableSource<T>, IConnectableObservableSource<U>> connectableSelector;

        readonly Func<IObservableSource<U>, IObservableSource<R>> handler;

        public ObservableSourceMulticastConnect(IObservableSource<T> source, Func<IObservableSource<T>, IConnectableObservableSource<U>> connectableSelector, Func<IObservableSource<U>, IObservableSource<R>> handler)
        {
            this.source = source;
            this.connectableSelector = connectableSelector;
            this.handler = handler;
        }

        public void Subscribe(ISignalObserver<R> observer)
        {
            var connectable = default(IConnectableObservableSource<U>);
            var observable = default(IObservableSource<R>);
            try
            {
                connectable = RequireNonNullRef(connectableSelector(source), "The subjectFactory returned a null IConnectableObservableSource");
                observable = RequireNonNullRef(handler(connectable), "The handler returned a null IObservableSource");
            }
            catch (Exception ex)
            {
                DisposableHelper.Error(observer, ex);
                return;
            }

            var parent = new MulticastObserver(observer);
            observer.OnSubscribe(parent);

            observable.Subscribe(parent);

            connectable.Connect(d => parent.SetConnection(d));
        }

        sealed class MulticastObserver : ISignalObserver<R>, IDisposable
        {
            readonly ISignalObserver<R> downstream;

            IDisposable upstream;

            IDisposable connection;

            public MulticastObserver(ISignalObserver<R> downstream)
            {
                this.downstream = downstream;
            }

            internal void SetConnection(IDisposable d)
            {
                DisposableHelper.SetOnce(ref connection, d);
            }

            public void Dispose()
            {
                DisposableHelper.Dispose(ref upstream);
                DisposableHelper.Dispose(ref connection);
            }

            public void OnCompleted()
            {
                DisposableHelper.Dispose(ref connection);
                downstream.OnCompleted();
            }

            public void OnError(Exception ex)
            {
                DisposableHelper.Dispose(ref connection);
                downstream.OnError(ex);
            }

            public void OnNext(R item)
            {
                downstream.OnNext(item);
            }

            public void OnSubscribe(IDisposable d)
            {
                DisposableHelper.SetOnce(ref upstream, d);
            }
        }
    }
}
