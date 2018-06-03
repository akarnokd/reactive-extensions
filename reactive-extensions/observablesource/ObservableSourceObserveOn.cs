using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    internal sealed class ObservableSourceObserveOn<T> : IObservableSource<T>
    {
        readonly IObservableSource<T> source;

        readonly IScheduler scheduler;

        readonly bool delayError;

        readonly int capacityHint;

        readonly bool fair;

        public ObservableSourceObserveOn(IObservableSource<T> source, IScheduler scheduler, bool delayError, int capacityHint, bool fair)
        {
            this.source = source;
            this.scheduler = scheduler;
            this.delayError = delayError;
            this.capacityHint = capacityHint;
            this.fair = fair;
        }

        public void Subscribe(ISignalObserver<T> observer)
        {
            source.Subscribe(new ObserveOnObserver(observer, scheduler, delayError, capacityHint, fair));
        }

        sealed class ObserveOnObserver : ISignalObserver<T>, IFuseableDisposable<T>
        {
            readonly ISignalObserver<T> downstream;

            readonly IScheduler scheduler;

            readonly bool delayError;

            readonly int capacityHint;

            readonly bool fair;

            IDisposable upstream;

            IDisposable task;

            bool done;
            Exception error;

            bool disposed;

            ISimpleQueue<T> queue;

            int sourceMode;

            bool outputFused;

            int wip;

            public ObserveOnObserver(ISignalObserver<T> downstream, IScheduler scheduler, bool delayError, int capacityHint, bool fair)
            {
                this.downstream = downstream;
                this.scheduler = scheduler;
                this.delayError = delayError;
                this.capacityHint = capacityHint;
                this.fair = fair;
            }

            public void Clear()
            {
                queue.Clear();
            }

            public void Dispose()
            {
                Volatile.Write(ref disposed, true);
                upstream.Dispose();
                DisposableHelper.Dispose(ref task);

                if (Interlocked.Increment(ref wip) == 1)
                {
                    do
                    {
                        queue.Clear();
                    } while (Interlocked.Decrement(ref wip) != 0);
                }
            }

            public bool IsEmpty()
            {
                return queue.IsEmpty();
            }

            public void OnCompleted()
            {
                Volatile.Write(ref done, true);
                Drain();
            }

            public void OnError(Exception ex)
            {
                error = ex;
                Volatile.Write(ref done, true);
                Drain();
            }

            public void OnNext(T item)
            {
                if (sourceMode == FusionSupport.None)
                {
                    queue.TryOffer(item);
                }
                Drain();
            }

            public void OnSubscribe(IDisposable d)
            {
                upstream = d;
                if (d is IFuseableDisposable<T> f)
                {
                    var m = f.RequestFusion(FusionSupport.AnyBoundary);

                    if (m == FusionSupport.Sync)
                    {
                        sourceMode = m;
                        queue = f;
                        Volatile.Write(ref done, true);
                        downstream.OnSubscribe(this);
                        Drain();
                        return;
                    }
                    if (m == FusionSupport.Async)
                    {
                        sourceMode = m;
                        queue = f;
                        downstream.OnSubscribe(this);
                        return;
                    }
                }

                queue = new SpscLinkedArrayQueue<T>(capacityHint);
                downstream.OnSubscribe(this);
            }

            public int RequestFusion(int mode)
            {
                if ((mode & FusionSupport.Async) != 0)
                {
                    outputFused = true;
                    return FusionSupport.Async;
                }
                return FusionSupport.None;
            }

            public bool TryOffer(T item)
            {
                throw new InvalidOperationException("Should not be called!");
            }

            public T TryPoll(out bool success)
            {
                return queue.TryPoll(out success);
            }

            void Drain()
            {
                if (Interlocked.Increment(ref wip) == 1)
                {
                    var sad = new SingleAssignmentDisposable();

                    if (DisposableHelper.Replace(ref task, sad))
                    {
                        sad.Disposable = scheduler.Schedule(this, (sch, @this) =>
                        {
                            return @this.Run(sch);
                        });
                    }
                }
            }

            IDisposable Run(IScheduler scheduler)
            {
                var fair = this.fair;

                if (outputFused)
                {
                    if (fair)
                    {
                        return DrainFusedFair(scheduler);
                    }
                    DrainFused();
                    return DisposableHelper.EMPTY;
                }
                if (sourceMode == FusionSupport.Sync)
                {
                    if (fair)
                    {
                        return DrainSyncFair(scheduler);
                    }

                    DrainSync();
                    return DisposableHelper.EMPTY;
                }
                if (fair)
                {
                    return DrainNormalFair(scheduler);
                }

                DrainNormal();
                return DisposableHelper.EMPTY;
            }

            void DrainFused()
            {
                var missed = 1;
                var delayError = this.delayError;
                var downstream = this.downstream;
                var queue = this.queue;

                for (; ;)
                {
                    if (Volatile.Read(ref disposed))
                    {
                        queue.Clear();
                    }
                    else
                    {
                        var d = Volatile.Read(ref done);

                        if (d && !delayError)
                        {
                            var ex = error;
                            if (ex != null)
                            {
                                downstream.OnError(ex);
                                Volatile.Write(ref disposed, true);
                                continue;
                            }
                        }

                        if (!queue.IsEmpty())
                        {
                            downstream.OnNext(default(T));
                        }

                        if (d)
                        {
                            var ex = error;
                            if (ex != null)
                            {
                                downstream.OnError(ex);
                            }
                            else
                            {
                                downstream.OnCompleted();
                            }
                            Volatile.Write(ref disposed, true);
                            continue;
                        }
                    }

                    missed = Interlocked.Add(ref wip, -missed);
                    if (missed == 0)
                    {
                        break;
                    }
                }
            }

            IDisposable DrainFusedFair(IScheduler scheduler)
            {
                var delayError = this.delayError;
                var downstream = this.downstream;
                var queue = this.queue;

                for (; ; )
                {
                    if (Volatile.Read(ref disposed))
                    {
                        queue.Clear();
                    }
                    else
                    {
                        var d = Volatile.Read(ref done);

                        if (d && !delayError)
                        {
                            var ex = error;
                            if (ex != null)
                            {
                                downstream.OnError(ex);
                                Volatile.Write(ref disposed, true);
                                continue;
                            }
                        }

                        if (!queue.IsEmpty())
                        {
                            downstream.OnNext(default(T));
                        }

                        if (d)
                        {
                            var ex = error;
                            if (ex != null)
                            {
                                downstream.OnError(ex);
                            }
                            else
                            {
                                downstream.OnCompleted();
                            }
                            Volatile.Write(ref disposed, true);
                            continue;
                        }
                    }

                    if (Interlocked.Decrement(ref wip) == 0)
                    {
                        return DisposableHelper.EMPTY;
                    }

                    return scheduler.Schedule(this, (sch, @this) => @this.Run(sch));
                }
            }

            void DrainSync()
            {
                var missed = 1;
                var downstream = this.downstream;
                var queue = this.queue;

                for (; ;)
                {
                    if (Volatile.Read(ref disposed))
                    {
                        queue.Clear();
                    }
                    else
                    {
                        var success = false;
                        var v = default(T);

                        try
                        {
                            v = queue.TryPoll(out success);
                        }
                        catch (Exception ex)
                        {
                            downstream.OnError(ex);
                            Volatile.Write(ref disposed, true);
                            continue;
                        }

                        if (success)
                        {
                            downstream.OnNext(v);
                            continue;
                        }
                        else
                        {
                            downstream.OnCompleted();
                            Volatile.Write(ref disposed, true);
                        }
                    }
                    missed = Interlocked.Add(ref wip, -missed);
                    if (missed == 0)
                    {
                        break;
                    }
                }
            }

            IDisposable DrainSyncFair(IScheduler scheduler)
            {
                var downstream = this.downstream;
                var queue = this.queue;

                for (; ; )
                {
                    if (Volatile.Read(ref disposed))
                    {
                        queue.Clear();
                    }
                    else
                    {
                        var success = false;
                        var v = default(T);

                        try
                        {
                            v = queue.TryPoll(out success);
                        }
                        catch (Exception ex)
                        {
                            downstream.OnError(ex);
                            Volatile.Write(ref disposed, true);
                            continue;
                        }

                        if (success)
                        {
                            downstream.OnNext(v);
                            continue;
                        }
                        else
                        {
                            downstream.OnCompleted();
                            Volatile.Write(ref disposed, true);
                        }
                    }

                    if (Interlocked.Decrement(ref wip) == 0)
                    {
                        return DisposableHelper.EMPTY;
                    }

                    return scheduler.Schedule(this, (sch, @this) => @this.Run(sch));
                }
            }

            void DrainNormal()
            {
                var missed = 1;
                var delayError = this.delayError;
                var downstream = this.downstream;
                var queue = this.queue;

                for (; ;)
                {
                    if (Volatile.Read(ref disposed))
                    {
                        queue.Clear();
                    }
                    else
                    {
                        var d = Volatile.Read(ref done);
                        if (d && !delayError)
                        {
                            var ex = error;
                            if (ex != null)
                            {
                                downstream.OnError(ex);
                                Volatile.Write(ref disposed, true);
                                continue;
                            }
                        }

                        var success = false;
                        var v = default(T);

                        try
                        {
                            v = queue.TryPoll(out success);
                        }
                        catch (Exception ex)
                        {
                            DisposableHelper.Dispose(ref upstream);
                            downstream.OnError(ex);
                            Volatile.Write(ref disposed, true);
                            continue;
                        }

                        if (d && !success)
                        {
                            var ex = error;
                            if (ex != null)
                            {
                                downstream.OnError(ex);
                            }
                            else
                            {
                                downstream.OnCompleted();
                            }
                            Volatile.Write(ref disposed, true);
                        }
                        if (success)
                        {
                            downstream.OnNext(v);
                            continue;
                        }
                    }

                    missed = Interlocked.Add(ref wip, -missed);
                    if (missed == 0)
                    {
                        break;
                    }
                }
            }

            IDisposable DrainNormalFair(IScheduler scheduler)
            {
                var delayError = this.delayError;
                var downstream = this.downstream;
                var queue = this.queue;

                for (; ; )
                {
                    if (Volatile.Read(ref disposed))
                    {
                        queue.Clear();
                    }
                    else
                    {
                        var d = Volatile.Read(ref done);
                        if (d && !delayError)
                        {
                            var ex = error;
                            if (ex != null)
                            {
                                downstream.OnError(ex);
                                Volatile.Write(ref disposed, true);
                                continue;
                            }
                        }

                        var success = false;
                        var v = default(T);

                        try
                        {
                            v = queue.TryPoll(out success);
                        }
                        catch (Exception ex)
                        {
                            DisposableHelper.Dispose(ref upstream);
                            downstream.OnError(ex);
                            Volatile.Write(ref disposed, true);
                            continue;
                        }

                        if (d && !success)
                        {
                            var ex = error;
                            if (ex != null)
                            {
                                downstream.OnError(ex);
                            }
                            else
                            {
                                downstream.OnCompleted();
                            }
                            Volatile.Write(ref disposed, true);
                        }
                        if (success)
                        {
                            downstream.OnNext(v);
                            continue;
                        }
                    }

                    if (Interlocked.Decrement(ref wip) == 0)
                    {
                        return DisposableHelper.EMPTY;
                    }

                    return scheduler.Schedule(this, (sch, @this) => @this.Run(sch));
                }
            }
        }
    }
}
