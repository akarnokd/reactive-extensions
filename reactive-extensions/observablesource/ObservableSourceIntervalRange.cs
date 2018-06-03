using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    internal sealed class ObservableSourceIntervalRange : IObservableSource<long>
    {
        readonly long start;

        readonly long end;

        readonly TimeSpan initialDelay;

        readonly TimeSpan period;

        readonly IScheduler scheduler;

        public ObservableSourceIntervalRange(long start, long end, TimeSpan initialDelay, TimeSpan period, IScheduler scheduler)
        {
            this.start = start;
            this.end = end;
            this.initialDelay = initialDelay;
            this.period = period;
            this.scheduler = scheduler;
        }

        public void Subscribe(ISignalObserver<long> observer)
        {
            var parent = new IntervalDisposable(observer, start, end);
            observer.OnSubscribe(parent);

            if (initialDelay == period)
            {
                var d = scheduler.SchedulePeriodic(parent, period, t =>
                {
                    t.Run();
                });

                parent.SetTask(d);
            }
            else
            {
                var d = scheduler.Schedule((parent, period), initialDelay, (sch, state) =>
                {
                    state.parent.Run();
                    return sch.SchedulePeriodic(state.parent, state.period, t =>
                    {
                        t.Run();
                    });
                });

                parent.SetTask(d);
            }
        }

        sealed class IntervalDisposable : IFuseableDisposable<long>
        {
            readonly ISignalObserver<long> downstream;

            readonly long end;

            long index;

            bool fused;

            long fusedReady;

            IDisposable task;

            public IntervalDisposable(ISignalObserver<long> downstream, long index, long end)
            {
                this.downstream = downstream;
                this.index = index;
                this.fusedReady = index;
                this.end = end;
            }

            internal void SetTask(IDisposable d)
            {
                DisposableHelper.Replace(ref task, d);
            }

            public void Clear()
            {
                index = Volatile.Read(ref fusedReady);
            }

            public void Dispose()
            {
                DisposableHelper.Dispose(ref task);
            }

            public bool IsEmpty()
            {
                return Volatile.Read(ref fusedReady) == index;
            }

            public int RequestFusion(int mode)
            {
                if ((mode & FusionSupport.Async) != 0)
                {
                    fused = true;
                    return FusionSupport.Async;
                }
                return FusionSupport.None;
            }

            public bool TryOffer(long item)
            {
                throw new InvalidOperationException("Should not be called!");
            }

            public long TryPoll(out bool success)
            {
                var ready = Volatile.Read(ref fusedReady);
                var idx = index;
                if (idx != ready)
                {
                    index = idx + 1;
                    success = true;
                    return idx;
                }
                success = false;
                return default(long);
            }

            internal void Run()
            {
                if (DisposableHelper.IsDisposed(ref task))
                {
                    return;
                }
                if (fused)
                {
                    var fr = fusedReady;
                    if (fr == end)
                    {
                        downstream.OnCompleted();
                        Dispose();
                    }
                    else
                    {
                        Volatile.Write(ref fusedReady, fr + 1);
                        downstream.OnNext(default(long));

                        if (fr + 1 == end)
                        {
                            downstream.OnCompleted();
                            Dispose();
                        }
                    }
                }
                else
                {
                    var idx = index;
                    if (idx == end)
                    {
                        downstream.OnCompleted();
                        Dispose();
                    }
                    else
                    {
                        index = idx + 1;
                        downstream.OnNext(idx);
                        if (idx + 1 == end)
                        {
                            downstream.OnCompleted();
                            Dispose();
                        }
                    }
                }
            }
        }
    }
}
