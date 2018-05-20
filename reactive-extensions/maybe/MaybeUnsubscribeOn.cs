using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// When the downstream disposes, the upstream's disposable
    /// is called from the given scheduler.
    /// </summary>
    /// <typeparam name="T">The success value type.</typeparam>
    /// <remarks>Since 0.0.11</remarks>
    internal sealed class MaybeUnsubscribeOn<T> : IMaybeSource<T>
    {
        readonly IMaybeSource<T> source;

        readonly IScheduler scheduler;

        public MaybeUnsubscribeOn(IMaybeSource<T> source, IScheduler scheduler)
        {
            this.source = source;
            this.scheduler = scheduler;
        }

        public void Subscribe(IMaybeObserver<T> observer)
        {
            source.Subscribe(new UnsubscribeOnObserver(observer, scheduler));
        }

        sealed class UnsubscribeOnObserver : IMaybeObserver<T>, IDisposable
        {
            readonly IMaybeObserver<T> downstream;

            readonly IScheduler scheduler;

            int disposed;

            IDisposable upstream;

            static readonly Func<IScheduler, UnsubscribeOnObserver, IDisposable> RUN =
                (s, t) => { t.Run(); return DisposableHelper.EMPTY; };

            public UnsubscribeOnObserver(IMaybeObserver<T> downstream, IScheduler scheduler)
            {
                this.downstream = downstream;
                this.scheduler = scheduler;
            }

            public void Dispose()
            {
                if (Interlocked.Exchange(ref disposed, 1) == 0)
                {
                    scheduler.Schedule(this, RUN);
                }
            }

            void Run()
            {
                upstream.Dispose();
            }

            public void OnCompleted()
            {
                if (Volatile.Read(ref disposed) == 0)
                {
                    downstream.OnCompleted();
                }
            }

            public void OnError(Exception error)
            {
                if (Volatile.Read(ref disposed) == 0)
                {
                    downstream.OnError(error);
                }
            }

            public void OnSuccess(T item)
            {
                if (Volatile.Read(ref disposed) == 0)
                {
                    downstream.OnSuccess(item);
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
