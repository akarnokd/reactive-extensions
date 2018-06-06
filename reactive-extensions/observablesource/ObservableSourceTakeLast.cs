using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    internal sealed class ObservableSourceTakeLast<T> : IObservableSource<T>
    {
        readonly IObservableSource<T> source;

        readonly int n;

        public ObservableSourceTakeLast(IObservableSource<T> source, int n)
        {
            this.source = source;
            this.n = n;
        }

        public void Subscribe(ISignalObserver<T> observer)
        {
            source.Subscribe(new TakeLastObserver(observer, n));
        }

        sealed class TakeLastObserver : IFuseableDisposable<T>, ISignalObserver<T>
        {
            readonly ISignalObserver<T> downstream;

            readonly int n;

            readonly Queue<T> queue;

            IDisposable upstream;

            int state;

            static readonly int StateFused = 1;
            static readonly int StateReady = 2;

            bool disposed;

            public TakeLastObserver(ISignalObserver<T> downstream, int n)
            {
                this.downstream = downstream;
                this.n = n;
                this.queue = new Queue<T>();
            }

            public void Clear()
            {
                if (Volatile.Read(ref state) == StateReady)
                {
                    queue.Clear();
                }
            }

            public void Dispose()
            {
                Volatile.Write(ref disposed, true);
                upstream.Dispose();
            }

            public bool IsEmpty()
            {
                if (Volatile.Read(ref state) == StateReady)
                {
                    return queue.Count == 0;
                }
                return true;
            }

            public void OnCompleted()
            {
                if (Volatile.Read(ref state) == StateFused)
                {
                    Volatile.Write(ref state, StateReady);
                    downstream.OnNext(default(T));
                    if (!Volatile.Read(ref disposed))
                    {
                        downstream.OnCompleted();
                    }
                }
                else
                {
                    var queue = this.queue;
                    while (queue.Count != 0)
                    {
                        if (Volatile.Read(ref disposed))
                        {
                            return;
                        }
                        downstream.OnNext(queue.Dequeue());
                    }
                    if (Volatile.Read(ref disposed))
                    {
                        return;
                    }
                    downstream.OnCompleted();
                }
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
                    queue.Dequeue();
                }
                queue.Enqueue(item);
            }

            public void OnSubscribe(IDisposable d)
            {
                upstream = d;
                downstream.OnSubscribe(this);
            }

            public int RequestFusion(int mode)
            {
                if ((mode & FusionSupport.Async) != 0)
                {
                    Volatile.Write(ref state, StateFused);
                    return FusionSupport.Async;
                }
                return FusionSupport.None;
            }

            public bool TryOffer(T item)
            {
                throw new InvalidOperationException("Should not be called!");
            }

            public T TryPoll(out bool success)
            {
                if (Volatile.Read(ref state) == StateReady)
                {
                    if (queue.Count != 0)
                    {
                        success = true;
                        return queue.Dequeue();
                    }
                }
                success = false;
                return default;
            }
        }
    }
}
