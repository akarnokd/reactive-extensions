using System;
using System.Collections.Generic;
using System.Text;

namespace akarnokd.reactive_extensions
{
    internal sealed class ObservableSourceOnErrorResumeNext<T> : IObservableSource<T>
    {
        readonly IObservableSource<T> source;

        readonly Func<Exception, IObservableSource<T>> handler;

        public ObservableSourceOnErrorResumeNext(IObservableSource<T> source, Func<Exception, IObservableSource<T>> handler)
        {
            this.source = source;
            this.handler = handler;
        }

        public void Subscribe(ISignalObserver<T> observer)
        {
            source.Subscribe(new OnErrorResumeNextObserver(observer, handler));
        }

        sealed class OnErrorResumeNextObserver : ISignalObserver<T>, IDisposable
        {
            readonly ISignalObserver<T> downstream;

            readonly Func<Exception, IObservableSource<T>> handler;

            IDisposable upstream;

            bool once;

            public OnErrorResumeNextObserver(ISignalObserver<T> downstream, Func<Exception, IObservableSource<T>> handler)
            {
                this.downstream = downstream;
                this.handler = handler;
            }

            public void Dispose()
            {
                DisposableHelper.Dispose(ref upstream);
            }

            public void OnCompleted()
            {
                downstream.OnCompleted();
            }

            public void OnError(Exception ex)
            {
                if (once)
                {
                    downstream.OnError(ex);
                }
                else
                {
                    var o = default(IObservableSource<T>);

                    try
                    {
                        o = ValidationHelper.RequireNonNullRef(handler(ex), "The handler returned a null IObservableSource");
                    }
                    catch (Exception exc)
                    {
                        downstream.OnError(new AggregateException(ex, exc));
                        return;
                    }
                    once = true;
                    o.Subscribe(this);
                }
            }

            public void OnNext(T item)
            {
                downstream.OnNext(item);
            }

            public void OnSubscribe(IDisposable d)
            {
                if (DisposableHelper.Replace(ref upstream, d))
                {
                    if (!once)
                    {
                        downstream.OnSubscribe(this);
                    }
                }
            }
        }
    }
}
