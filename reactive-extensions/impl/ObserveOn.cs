using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    sealed class ObserveOn<T> : IObservable<T>
    {
        readonly IObservable<T> source;

        readonly IScheduler scheduler;

        readonly bool delayError;

        public ObserveOn(IObservable<T> source, IScheduler scheduler, bool delayError)
        {
            this.source = source;
            this.scheduler = scheduler;
            this.delayError = delayError;
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            var parent = new ObserveOnObserver(observer, scheduler, delayError);
            var d = source.Subscribe(parent);
            parent.OnSubscribe(d);
            return parent;
        }

        sealed class ObserveOnObserver : BaseObserver<T, T>
        {
            readonly IScheduler scheduler;

            readonly bool delayError;

            readonly SpscLinkedArrayQueue<T> queue;

            int wip;

            Exception error;

            bool done;

            static readonly Func<IScheduler, ObserveOnObserver, IDisposable> TASK;

            static ObserveOnObserver()
            {
                TASK = (sch, state) => { state.Run(); return DisposableHelper.EMPTY; };
            }

            public ObserveOnObserver(IObserver<T> downstream, IScheduler scheduler, bool delayError) : base(downstream)
            {
                this.scheduler = scheduler;
                this.delayError = delayError;
                this.queue = new SpscLinkedArrayQueue<T>(128);
            }

            public override void OnCompleted()
            {
                Volatile.Write(ref done, true);
                Schedule();
            }

            public override void OnError(Exception error)
            {
                this.error = error;
                Volatile.Write(ref done, true);
                Schedule();
            }

            public override void OnNext(T value)
            {
                queue.Offer(value);
                Schedule();
            }

            void Schedule()
            {
                if (Interlocked.Increment(ref wip) == 1)
                {
                    scheduler.Schedule(this, TASK);
                }
            }

            void Run()
            {
                var downstream = this.downstream;
                var queue = this.queue;
                var delayError = this.delayError;
                int missed = 1;

                for (; ; )
                {
                    for (; ; )
                    {
                        if (IsDisposed())
                        {
                            queue.Clear();
                            break;
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
                                    Dispose();
                                    continue;
                                }
                            }

                            var empty = !queue.TryPoll(out var v);

                            if (d && empty)
                            {
                                var ex = error;
                                if (ex != null)
                                {
                                    downstream.OnError(ex);
                                }
                                else
                                {
                                    downstream.OnCompleted();
                                    Dispose();
                                    continue;
                                }
                            }

                            if (empty)
                            {
                                break;
                            }

                            downstream.OnNext(v);
                        }
                    }

                    missed = Interlocked.Add(ref wip, -missed);
                    if (missed == 0)
                    {
                        break;
                    }
                }
            }
        }
    }
}
