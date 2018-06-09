using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    internal sealed class ObservableSourceDelay<T> : IObservableSource<T>
    {
        readonly IObservableSource<T> source;

        readonly TimeSpan delay;

        readonly IScheduler scheduler;

        readonly bool delayError;

        public ObservableSourceDelay(IObservableSource<T> source, TimeSpan delay, IScheduler scheduler, bool delayError)
        {
            this.source = source;
            this.delay = delay;
            this.scheduler = scheduler;
            this.delayError = delayError;
        }

        public void Subscribe(ISignalObserver<T> observer)
        {
            source.Subscribe(new DelayObserver(observer, delay, scheduler, delayError));
        }

        sealed class DelayObserver : ISignalObserver<T>, IDisposable
        {
            readonly ISignalObserver<T> downstream;

            readonly TimeSpan delay;

            readonly IScheduler scheduler;

            readonly IStopwatch stopwatch;

            readonly ConcurrentQueue<(TimeSpan due, T item, bool done)> queue;

            readonly bool delayError;

            IDisposable upstream;

            IDisposable task;

            Exception error;

            int wip;

            public DelayObserver(ISignalObserver<T> downstream, TimeSpan delay, IScheduler scheduler, bool delayError)
            {
                this.downstream = downstream;
                this.delay = delay;
                this.scheduler = scheduler;
                this.stopwatch = scheduler.StartStopwatch();
                this.queue = new ConcurrentQueue<(TimeSpan due, T item, bool done)>();
                this.delayError = delayError;
            }

            public void Dispose()
            {
                upstream.Dispose();
                DisposableHelper.Dispose(ref task);
                while (queue.TryDequeue(out var _)) ;
            }

            public void OnCompleted()
            {
                queue.Enqueue((stopwatch.Elapsed + delay, default, true));
                Schedule();
            }

            public void OnError(Exception ex)
            {
                Volatile.Write(ref error, ex);
                if (delayError)
                {
                    queue.Enqueue((TimeSpan.Zero, default, true));
                }
                else
                {
                    queue.Enqueue((stopwatch.Elapsed + delay, default, true));
                }
                Schedule();
            }

            public void OnNext(T item)
            {
                queue.Enqueue((stopwatch.Elapsed + delay, item, false));
                Schedule();
            }

            void Schedule()
            {
                if (Interlocked.Increment(ref wip) == 1)
                {
                    var sad = new SingleAssignmentDisposable();
                    if (DisposableHelper.Replace(ref task, sad))
                    {
                        sad.Disposable = scheduler.Schedule(this, delay, (s, @this) => @this.Run(s));
                    }
                }
            }

            public void OnSubscribe(IDisposable d)
            {
                upstream = d;
                downstream.OnSubscribe(this);
            }


            IDisposable Run(IScheduler scheduler)
            {
                for (; ; )
                {
                    if (DisposableHelper.IsDisposed(ref task))
                    {
                        while (queue.TryDequeue(out var _)) ;
                        return DisposableHelper.EMPTY;
                    }
                    else
                    {
                        if (!delayError)
                        {
                            var ex = Volatile.Read(ref error);
                            if (ex != null)
                            {
                                downstream.OnError(ex);
                                while (queue.TryDequeue(out var _)) ;
                                return DisposableHelper.EMPTY;
                            }
                        }


                        if (queue.TryPeek(out var entry))
                        {
                            var now = stopwatch.Elapsed;

                            if (entry.due > now)
                            {
                                return scheduler.Schedule(this, entry.due - now, (s, @this) => @this.Run(s));
                            }

                            if (entry.done)
                            {
                                var ex = Volatile.Read(ref error);
                                if (ex != null)
                                {
                                    downstream.OnError(ex);
                                }
                                else
                                {
                                    downstream.OnCompleted();
                                }
                                return DisposableHelper.EMPTY;
                            }

                            queue.TryDequeue(out var _);

                            downstream.OnNext(entry.item);
                        }
                        else
                        {
                            if (Interlocked.Decrement(ref wip) == 0)
                            {
                                return DisposableHelper.EMPTY;
                            } 
                        }
                    }
                }
            }
        }
    }
}
