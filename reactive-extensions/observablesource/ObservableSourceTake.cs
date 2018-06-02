using System;
using System.Collections.Generic;
using System.Text;

namespace akarnokd.reactive_extensions
{
    internal sealed class ObservableSourceTake<T> : IObservableSource<T>
    {
        readonly IObservableSource<T> source;

        readonly long n;

        public ObservableSourceTake(IObservableSource<T> source, long n)
        {
            this.source = source;
            this.n = n;
        }

        public void Subscribe(ISignalObserver<T> observer)
        {
            source.Subscribe(new TakeObserver(observer, n));
        }

        sealed class TakeObserver : ISignalObserver<T>, IDisposable
        {
            readonly ISignalObserver<T> downstream;

            IDisposable upstream;

            long remaining;

            public TakeObserver(ISignalObserver<T> downstream, long remaining)
            {
                this.downstream = downstream;
                this.remaining = remaining;
            }

            public void Dispose()
            {
                upstream.Dispose();
            }

            public void OnCompleted()
            {
                if (remaining > 0)
                {
                    downstream.OnCompleted();
                }
            }

            public void OnError(Exception ex)
            {
                if (remaining > 0)
                {
                    downstream.OnError(ex);
                }
                // TODO what to do in this case?
            }

            public void OnNext(T item)
            {
                long r = remaining;
                if (r > 0)
                {
                    remaining = --r;
                    downstream.OnNext(item);

                    if (r == 0L)
                    {
                        upstream.Dispose();
                        downstream.OnCompleted();
                    }
                }
            }

            public void OnSubscribe(IDisposable d)
            {
                upstream = d;
                if (remaining <= 0)
                {
                    d.Dispose();
                    DisposableHelper.Complete(downstream);
                }
                else
                {
                    downstream.OnSubscribe(this);
                }
            }
        }
    }
}
