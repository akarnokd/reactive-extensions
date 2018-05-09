using System;
using System.Collections.Generic;
using System.Text;

namespace akarnokd.reactive_extensions
{
    internal sealed class DoAfterTerminate<T> : IObservable<T>
    {
        readonly IObservable<T> source;

        readonly Action handler;

        public DoAfterTerminate(IObservable<T> source, Action handler)
        {
            this.source = source;
            this.handler = handler;
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            return source.Subscribe(new DoAfterTerminateObserver(observer, handler));
        }

        sealed class DoAfterTerminateObserver : IObserver<T>
        {
            readonly IObserver<T> downstream;

            readonly Action handler;

            public DoAfterTerminateObserver(IObserver<T> downstream, Action handler)
            {
                this.downstream = downstream;
                this.handler = handler;
            }

            public void OnCompleted()
            {
                try
                {
                    downstream.OnCompleted();
                }
                finally
                {
                    handler();
                }
            }

            public void OnError(Exception error)
            {
                try
                {
                    downstream.OnError(error);
                }
                finally
                {
                    handler();
                }
            }

            public void OnNext(T value)
            {
                downstream.OnNext(value);
            }
        }
    }
}
