using System;
using System.Collections.Generic;
using System.Text;

namespace akarnokd.reactive_extensions
{
    internal sealed class ObservableSourceTakeWhile<T> : IObservableSource<T>
    {
        readonly IObservableSource<T> source;

        readonly Func<T, bool> predicate;

        public ObservableSourceTakeWhile(IObservableSource<T> source, Func<T, bool> predicate)
        {
            this.source = source;
            this.predicate = predicate;
        }

        public void Subscribe(ISignalObserver<T> observer)
        {
            source.Subscribe(new TakeWhileObserver(observer, predicate));
        }

        sealed class TakeWhileObserver : ISignalObserver<T>, IDisposable
        {
            readonly ISignalObserver<T> downstream;

            readonly Func<T, bool> predicate;

            IDisposable upstream;

            bool done;

            public TakeWhileObserver(ISignalObserver<T> downstream, Func<T, bool> predicate)
            {
                this.downstream = downstream;
                this.predicate = predicate;
            }

            public void Dispose()
            {
                upstream.Dispose();
            }

            public void OnCompleted()
            {
                if (done)
                {
                    return;
                }
                done = true;
                downstream.OnCompleted();
            }

            public void OnError(Exception ex)
            {
                if (done)
                {
                    return;
                }
                done = true;
                downstream.OnError(ex);
            }

            public void OnNext(T item)
            {
                if (done)
                {
                    return;
                }
                var b = false;
                try
                {
                    b = predicate(item);
                }
                catch (Exception ex)
                {
                    upstream.Dispose();
                    OnError(ex);
                    return;
                }

                if (!b)
                {
                    upstream.Dispose();
                    OnCompleted();
                }
                else
                {
                    downstream.OnNext(item);
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
