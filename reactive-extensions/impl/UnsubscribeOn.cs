using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Makes sure the Dispose() call towards the upstream happens on the specified
    /// scheduler.
    /// </summary>
    /// <typeparam name="T">The element type of the sequence.</typeparam>
    internal sealed class UnsubscribeOn<T> : BaseObservable<T, T>
    {
        readonly IScheduler scheduler;

        internal UnsubscribeOn(IObservable<T> source, IScheduler scheduler) : base(source)
        {
            this.scheduler = scheduler;
        }

        protected override BaseObserver<T, T> CreateObserver(IObserver<T> observer)
        {
            return new UnsubscribeOnObserver(observer, scheduler);
        }

        sealed class UnsubscribeOnObserver : BaseObserver<T, T>
        {
            IScheduler scheduler;

            static readonly Func<IScheduler, IDisposable, IDisposable> TASK =
                (s, self) =>
                {
                    self.Dispose();
                    return DisposableHelper.EMPTY;
                };

            internal UnsubscribeOnObserver(IObserver<T> downstream, IScheduler scheduler) : base(downstream)
            {
                Volatile.Write(ref this.scheduler, scheduler);
            }

            public override void OnCompleted()
            {
                if (Volatile.Read(ref scheduler) != null)
                {
                    downstream.OnCompleted();
                    Dispose();
                }
            }

            public override void OnError(Exception error)
            {
                if (Volatile.Read(ref scheduler) != null)
                {
                    downstream.OnError(error);
                    Dispose();
                }
            }

            public override void OnNext(T value)
            {
                if (Volatile.Read(ref scheduler) != null) {
                    downstream.OnNext(value);
                }
            }

            public override void Dispose()
            {
                var d = Volatile.Read(ref upstream);
                if (d != DisposableHelper.DISPOSED)
                {
                    d = Interlocked.Exchange(ref upstream, DisposableHelper.DISPOSED);
                    if (d != null && d != DisposableHelper.DISPOSED)
                    {
                        Interlocked.Exchange(ref scheduler, null)?.Schedule(d, TASK);
                    }
                }
            }

            internal override void OnSubscribe(IDisposable d)
            {
                if (Interlocked.CompareExchange(ref upstream, d, null) != null)
                {
                    Interlocked.Exchange(ref scheduler, null)?.Schedule(d, TASK);
                }
            }

            void DisposeActual()
            {
                DisposableHelper.Dispose(ref upstream);
            }
        }
    }
}
