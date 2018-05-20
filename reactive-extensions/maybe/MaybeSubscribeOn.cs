using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Subscribes to the source on the given scheduler.
    /// </summary>
    /// <typeparam name="T">The success value type.</typeparam>
    /// <remarks>Since 0.0.11</remarks>
    internal sealed class MaybeSubscribeOn<T> : IMaybeSource<T>
    {
        readonly IMaybeSource<T> source;

        readonly IScheduler scheduler;

        public MaybeSubscribeOn(IMaybeSource<T> source, IScheduler scheduler)
        {
            this.source = source;
            this.scheduler = scheduler;
        }

        public void Subscribe(IMaybeObserver<T> observer)
        {
            var parent = new SubscribeOnObserver(observer, source);
            observer.OnSubscribe(parent);
            var d = scheduler.Schedule(parent, RUN);
            parent.SetTask(d);
        }

        static readonly Func<IScheduler, SubscribeOnObserver, IDisposable> RUN =
            (s, t) => { t.Run(); return DisposableHelper.EMPTY; };

        sealed class SubscribeOnObserver : IMaybeObserver<T>, IDisposable
        {
            readonly IMaybeObserver<T> downstream;

            IMaybeSource<T> source;

            IDisposable upstream;

            IDisposable task;

            public SubscribeOnObserver(IMaybeObserver<T> downstream, IMaybeSource<T> source)
            {
                this.downstream = downstream;
                this.source = source;
            }

            public void Dispose()
            {
                DisposableHelper.Dispose(ref upstream);
                DisposableHelper.Dispose(ref task);
            }

            public void OnCompleted()
            {
                downstream.OnCompleted();
            }

            public void OnError(Exception error)
            {
                downstream.OnError(error);
            }

            public void OnSuccess(T item)
            {
                downstream.OnSuccess(item);
            }

            public void OnSubscribe(IDisposable d)
            {
                DisposableHelper.SetOnce(ref upstream, d);
            }

            internal void SetTask(IDisposable d)
            {
                if (Interlocked.CompareExchange(ref task, d, null) != null)
                {
                    if (DisposableHelper.IsDisposed(ref task))
                    {
                        d.Dispose();
                    }
                }
            }

            internal void Run()
            {
                for (; ; )
                {
                    var d = Volatile.Read(ref task);
                    if (d == DisposableHelper.DISPOSED)
                    {
                        break;
                    }

                    if (Interlocked.CompareExchange(ref task, DisposableHelper.EMPTY, d) == d)
                    {
                        var s = source;
                        source = null;
                        s.Subscribe(this);
                        break;
                    }
                }
            }
        }
    }
}
