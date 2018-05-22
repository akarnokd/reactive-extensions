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
    internal sealed class SingleSubscribeOn<T> : ISingleSource<T>
    {
        readonly ISingleSource<T> source;

        readonly IScheduler scheduler;

        public SingleSubscribeOn(ISingleSource<T> source, IScheduler scheduler)
        {
            this.source = source;
            this.scheduler = scheduler;
        }

        public void Subscribe(ISingleObserver<T> observer)
        {
            var parent = new SubscribeOnObserver(observer, source);
            observer.OnSubscribe(parent);
            var d = scheduler.Schedule(parent, RUN);
            parent.SetTask(d);
        }

        static readonly Func<IScheduler, SubscribeOnObserver, IDisposable> RUN =
            (s, t) => { t.Run(); return DisposableHelper.EMPTY; };

        sealed class SubscribeOnObserver : ISingleObserver<T>, IDisposable
        {
            readonly ISingleObserver<T> downstream;

            ISingleSource<T> source;

            IDisposable upstream;

            IDisposable task;

            public SubscribeOnObserver(ISingleObserver<T> downstream, ISingleSource<T> source)
            {
                this.downstream = downstream;
                this.source = source;
            }

            public void Dispose()
            {
                DisposableHelper.Dispose(ref upstream);
                DisposableHelper.Dispose(ref task);
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
