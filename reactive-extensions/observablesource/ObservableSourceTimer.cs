using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    internal sealed class ObservableSourceTimer : IObservableSource<long>
    {
        readonly TimeSpan delay;

        readonly IScheduler scheduler;

        public ObservableSourceTimer(TimeSpan delay, IScheduler scheduler)
        {
            this.delay = delay;
            this.scheduler = scheduler;
        }

        public void Subscribe(ISignalObserver<long> observer)
        {
            var parent = new TimerDisposable(observer);
            observer.OnSubscribe(parent);

            var d = scheduler.Schedule(parent, delay, (_, @this) => { @this.Run(); return DisposableHelper.EMPTY; });
            parent.SetTask(d);
        }

        sealed class TimerDisposable : IFuseableDisposable<long>
        {
            readonly ISignalObserver<long> downstream;

            IDisposable task;

            int state;

            static readonly int Empty = 0;
            static readonly int FusedEmpty = 4;
            static readonly int FusedReady = 5;
            static readonly int Disposed = 6;

            public TimerDisposable(ISignalObserver<long> downstream)
            {
                this.downstream = downstream;
            }

            internal void SetTask(IDisposable d)
            {
                DisposableHelper.Replace(ref task, d);
            }

            public void Clear()
            {
                Volatile.Write(ref state, Disposed);
            }

            public void Dispose()
            {
                Volatile.Write(ref state, Disposed);
                DisposableHelper.Dispose(ref task);
            }

            public bool IsEmpty()
            {
                return Volatile.Read(ref state) != FusedReady;
            }

            public int RequestFusion(int mode)
            {
                if ((mode & FusionSupport.Async) != 0)
                {
                    Volatile.Write(ref state, FusedEmpty);
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
                var s = Volatile.Read(ref state);
                if (s == FusedReady)
                {
                    success = true;
                    Volatile.Write(ref state, Disposed);
                    return 0L;
                }

                success = false;
                return default(long);
            }

            internal void Run()
            {
                var s = Volatile.Read(ref state);
                if (s == Empty)
                {
                    downstream.OnNext(0L);
                    if (Volatile.Read(ref state) != Disposed)
                    {
                        Volatile.Write(ref state, Disposed);
                        downstream.OnCompleted();
                    }
                }
                else
                if (s == FusedEmpty)
                {
                    Volatile.Write(ref state, FusedReady);
                    downstream.OnNext(default(long));
                    downstream.OnCompleted();
                }
            }
        }
    }
}
