using System;

namespace akarnokd.reactive_extensions
{
    internal sealed class DoAfterNext<T> : IObservable<T>
    {
        readonly IObservable<T> source;

        readonly Action<T> handler;

        public DoAfterNext(IObservable<T> source, Action<T> handler)
        {
            this.source = source;
            this.handler = handler;
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            return source.Subscribe(new DoAfterNextObserver(observer, handler));
        }

        sealed class DoAfterNextObserver : IObserver<T>
        {
            readonly IObserver<T> downstream;

            readonly Action<T> handler;

            bool done;

            public DoAfterNextObserver(IObserver<T> downstream, Action<T> handler)
            {
                this.downstream = downstream;
                this.handler = handler;
            }

            public void OnCompleted()
            {
                if (done)
                {
                    return;
                }
                downstream.OnCompleted();
            }

            public void OnError(Exception error)
            {
                if (done)
                {
                    return;
                }
                downstream.OnError(error);
            }

            public void OnNext(T value)
            {
                if (done)
                {
                    return;
                }
                downstream.OnNext(value);
                try
                {
                    handler(value);
                }
                catch (Exception ex)
                {
                    done = true;
                    downstream.OnError(ex);
                }
            }
        }
    }
}
