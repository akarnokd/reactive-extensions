using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    internal sealed class ObservableSourceSkipLastTimed<T> : IObservableSource<T>
    {
        readonly IObservableSource<T> source;

        readonly TimeSpan timespan;

        readonly IScheduler scheduler;

        public ObservableSourceSkipLastTimed(IObservableSource<T> source, TimeSpan timespan, IScheduler scheduler)
        {
            this.source = source;
            this.timespan = timespan;
            this.scheduler = scheduler;
        }

        public void Subscribe(ISignalObserver<T> observer)
        {
            source.Subscribe(new SkipLastTimedObserver(observer, timespan, scheduler.StartStopwatch()));
        }

        sealed class SkipLastTimedObserver : ISignalObserver<T>, IDisposable
        {
            readonly ISignalObserver<T> downstream;

            readonly TimeSpan timespan;

            readonly IStopwatch stopwatch;

            Queue<(T item, TimeSpan time)> queue;

            IDisposable upstream;

            public SkipLastTimedObserver(ISignalObserver<T> downstream, TimeSpan timespan, IStopwatch stopwatch)
            {
                this.downstream = downstream;
                this.timespan = timespan;
                this.stopwatch = stopwatch;
                Volatile.Write(ref this.queue, new Queue<(T item, TimeSpan time)>());
            }

            public void Dispose()
            {
                Volatile.Write(ref queue, null);
                upstream.Dispose();
            }

            public void OnCompleted()
            {
                Volatile.Write(ref queue, null);
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
                            downstream.OnNext(entry.item);
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
