using System;
using System.Collections.Generic;
using System.Text;

namespace akarnokd.reactive_extensions
{
    internal sealed class ObservableSourceSkipLast<T> : IObservableSource<T>
    {
        readonly IObservableSource<T> source;

        readonly int n;

        public ObservableSourceSkipLast(IObservableSource<T> source, int n)
        {
            this.source = source;
            this.n = n;
        }

        public void Subscribe(ISignalObserver<T> observer)
        {
            source.Subscribe(new SkipLastObserver(observer, n));
        }

        sealed class SkipLastObserver : ISignalObserver<T>, IDisposable
        {
            readonly ISignalObserver<T> downstream;

            readonly int n;

            readonly Queue<T> queue;

            IDisposable upstream;

            public SkipLastObserver(ISignalObserver<T> downstream, int n)
            {
                this.downstream = downstream;
                this.n = n;
                this.queue = new Queue<T>();
            }

            public void Dispose()
            {
                upstream.Dispose();
            }

            public void OnCompleted()
            {
                queue.Clear();
                downstream.OnCompleted();
            }

            public void OnError(Exception ex)
            {
                queue.Clear();
                downstream.OnError(ex);
            }

            public void OnNext(T item)
            {
                if (queue.Count == n)
                {
                    downstream.OnNext(queue.Dequeue());
                }
                queue.Enqueue(item);
            }

            public void OnSubscribe(IDisposable d)
            {
                upstream = d;
                downstream.OnSubscribe(this);
            }
        }
    }
}
