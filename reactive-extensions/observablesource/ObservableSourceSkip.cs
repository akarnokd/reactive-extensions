using System;
using System.Collections.Generic;
using System.Text;

namespace akarnokd.reactive_extensions
{
    internal sealed class ObservableSourceSkip<T> : IObservableSource<T>
    {
        readonly IObservableSource<T> source;

        readonly long n;

        public ObservableSourceSkip(IObservableSource<T> source, long n)
        {
            this.source = source;
            this.n = n;
        }

        public void Subscribe(ISignalObserver<T> observer)
        {
            source.Subscribe(new SkipObserver(observer, n));
        }

        sealed class SkipObserver : ISignalObserver<T>, IDisposable
        {
            readonly ISignalObserver<T> downstream;

            long remaining;

            IDisposable upstream;

            public SkipObserver(ISignalObserver<T> downstream, long remaining)
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
                downstream.OnCompleted();
            }

            public void OnError(Exception ex)
            {
                downstream.OnError(ex);
            }

            public void OnNext(T item)
            {
                var r = remaining;
                if (r == 0)
                {
                    downstream.OnNext(item);
                }
                else
                {
                    remaining = r - 1;
                }
            }

            public void OnSubscribe(IDisposable d)
            {
                upstream = d;
                downstream.OnSubscribe(this);
            }
        }
    }
}
