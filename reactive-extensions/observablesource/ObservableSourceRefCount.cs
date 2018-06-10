using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Text;

namespace akarnokd.reactive_extensions
{
    internal sealed class ObservableSourceRefCount<T> : IObservableSource<T>
    {
        readonly IConnectableObservableSource<T> source;

        readonly int minObservers;

        readonly TimeSpan timeout;

        readonly IScheduler scheduler;

        int count;

        IDisposable connection;

        IDisposable task;

        public ObservableSourceRefCount(IConnectableObservableSource<T> source, int minObservers, TimeSpan timeout, IScheduler scheduler)
        {
            this.source = source;
            this.minObservers = minObservers;
            this.timeout = timeout;
            this.scheduler = scheduler;
        }

        public void Subscribe(ISignalObserver<T> observer)
        {

            var connectionDisposable = default(SingleAssignmentDisposable);
            var taskDisposable = default(IDisposable);
            lock (this)
            {
                var t = task;
                if (t != null)
                {
                    taskDisposable = t;
                    task = null;
                }

                if (++count == minObservers)
                {
                    connectionDisposable = new SingleAssignmentDisposable();
                    connection = connectionDisposable;
                }
            }

            if (taskDisposable != null)
            {
                taskDisposable.Dispose();
            }

            source.Subscribe(new RefCountObserver(observer, this));

            if (connectionDisposable != null)
            {
                source.Connect(d => connectionDisposable.Disposable = d);
            }
        }

        void Dispose()
        {
            var taskSad = default(SingleAssignmentDisposable);
            var dispose = default(IDisposable);
            lock (this)
            {
                if (--count == 0)
                {
                    if (scheduler != null)
                    {
                        taskSad = new SingleAssignmentDisposable();
                        task = taskSad;
                    }
                    else
                    {
                        dispose = connection;
                        connection = null;
                    }
                }
            }

            if (taskSad != null)
            {
                taskSad.Disposable = scheduler.Schedule((this, taskSad), timeout, (_, @this) => { @this.Item1.TerminateTimeout(@this.taskSad); return DisposableHelper.EMPTY; });
            }
            else
            {
                if (dispose != null)
                {
                    dispose.Dispose();
                    source.Reset();
                }
            }
        }

        void TerminateTimeout(IDisposable task)
        {
            var conn = default(IDisposable);
            lock (this)
            {
                if (this.task != task || count != 0)
                {
                    return;
                }
                this.task = null;
                conn = this.connection;
                this.connection = null;
            }
            conn?.Dispose();
            source.Reset();
        }

        void Terminate()
        {
            var d = default(IDisposable);
            lock (this)
            {
                count = 0;
                connection = null;
                d = task;
                task = null;
            }
            d?.Dispose();
            source.Reset();
        }

        sealed class RefCountObserver : ISignalObserver<T>, IDisposable
        {
            readonly ISignalObserver<T> downstream;

            readonly ObservableSourceRefCount<T> parent;

            IDisposable upstream;

            public RefCountObserver(ISignalObserver<T> downstream, ObservableSourceRefCount<T> parent)
            {
                this.downstream = downstream;
                this.parent = parent;
            }

            public void Dispose()
            {
                upstream.Dispose();
                parent.Dispose();
            }

            public void OnCompleted()
            {
                downstream.OnCompleted();
                parent.Terminate();
            }

            public void OnError(Exception ex)
            {
                downstream.OnError(ex);
                parent.Terminate();
            }

            public void OnNext(T item)
            {
                downstream.OnNext(item);
            }

            public void OnSubscribe(IDisposable d)
            {
                upstream = d;
                downstream.OnSubscribe(this);
            }
        }
    }
}
