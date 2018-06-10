using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    internal sealed class ObservableSourceTakeLastTimed<T> : IObservableSource<T>
    {
        readonly IObservableSource<T> source;

        readonly TimeSpan timespan;

        readonly IScheduler scheduler;

        public ObservableSourceTakeLastTimed(IObservableSource<T> source, TimeSpan timespan, IScheduler scheduler)
        {
            this.source = source;
            this.timespan = timespan;
            this.scheduler = scheduler;
        }

        public void Subscribe(ISignalObserver<T> observer)
        {
            source.Subscribe(new TakeLastTimedObserver(observer, timespan, scheduler.StartStopwatch()));
        }

        sealed class TakeLastTimedObserver : ISignalObserver<T>, IDisposable
        {
            readonly ISignalObserver<T> downstream;

            readonly TimeSpan timespan;

            readonly IStopwatch stopwatch;

            IDisposable upstream;

            Queue<(T item, TimeSpan time)> queue;

            public TakeLastTimedObserver(ISignalObserver<T> downstream, TimeSpan timespan, IStopwatch stopwatch)
            {
                this.downstream = downstream;
                this.timespan = timespan;
                this.stopwatch = stopwatch;
                Volatile.Write(ref queue, new Queue<(T item, TimeSpan time)>());
            }

            public void Dispose()
            {
                Volatile.Write(ref queue, null);
                upstream.Dispose();
            }

            public void OnCompleted()
            {
                var now = stopwatch.Elapsed;
                for (; ; )
                {
                    var q = Volatile.Read(ref queue);
                    if (q == null)
                    {
                        return;
                    }
                    if (q.Count == 0)
                    {
                        break;
                    }
                    var entry = q.Dequeue();
                    if (entry.time > now)
                    {
                        downstream.OnNext(entry.item);
                    }
                }
                downstream.OnCompleted();
            }

            public void OnError(Exception ex)
            {
                Volatile.Write(ref queue, null);
                downstream.OnError(ex);
            }

            public void OnNext(T item)
            {
                var q = Volatile.Read(ref queue);
                if (q != null)
                {
                    var now = stopwatch.Elapsed;
                    if (q.Count != 0)
                    {
                        var entry = q.Peek();
                        if (entry.time <= now)
                        {
                            q.Dequeue();
                        }
                    }
                    q.Enqueue((item, now + timespan));
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
