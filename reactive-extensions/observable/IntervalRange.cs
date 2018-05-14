using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Text;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Emit a range of longs over a period of time.
    /// </summary>
    internal sealed class IntervalRange : IObservable<long>
    {
        readonly long start;

        readonly long end;

        readonly TimeSpan initialDelay;

        readonly TimeSpan period;

        readonly IScheduler scheduler;

        internal static readonly Func<IScheduler, IntervalRangeDisposable, IDisposable> TASK =
            (scheduler, self) => { self.Run(scheduler); return DisposableHelper.EMPTY; };

        public IntervalRange(long start, long end, TimeSpan initialDelay, TimeSpan period, IScheduler scheduler)
        {
            this.start = start;
            this.end = end;
            this.initialDelay = initialDelay;
            this.period = period;
            this.scheduler = scheduler;
        }

        public IDisposable Subscribe(IObserver<long> observer)
        {
            var now = scheduler.Now + initialDelay;
            var parent = new IntervalRangeDisposable(observer, start, end, now, period);

            var sad = new SingleAssignmentDisposable();
            parent.SetTask(sad);

            sad.Disposable = scheduler.Schedule(parent, initialDelay, TASK);

            return parent;
        }

        internal sealed class IntervalRangeDisposable : IDisposable
        {
            readonly IObserver<long> downstream;

            readonly TimeSpan period;

            readonly long end;

            readonly DateTimeOffset startTime;

            IDisposable task;

            long index;

            long count;

            public IntervalRangeDisposable(IObserver<long> downstream, long start, long end, DateTimeOffset startTime, TimeSpan period)
            {
                this.downstream = downstream;
                this.period = period;
                this.index = start;
                this.startTime = startTime;
                this.end = end;
            }

            public void SetTask(IDisposable d)
            {
                DisposableHelper.Replace(ref task, d);
            }

            public void Dispose()
            {
                DisposableHelper.Dispose(ref task);
            }

            internal void Run(IScheduler scheduler)
            {
                var idx = index;
                if (idx != end)
                {
                    downstream.OnNext(idx++);
                }
                if (idx == end)
                {
                    downstream.OnCompleted();
                    return;
                }
                else
                {
                    index = idx;
                }

                var now = scheduler.Now;
                var next = startTime + TimeSpan.FromTicks(period.Ticks * (++count));

                var delay = next - now;

                var sad = new SingleAssignmentDisposable();
                SetTask(sad);

                sad.Disposable = scheduler.Schedule(this, delay, TASK);
            }
        }
    }
}
