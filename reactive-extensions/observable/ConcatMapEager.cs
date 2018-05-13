using System;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    internal sealed class ConcatMapEager<T, R> : BaseObservable<T, R>
    {
        readonly Func<T, IObservable<R>> mapper;

        readonly int maxConcurrency;

        readonly int capacityHint;

        internal ConcatMapEager(
            IObservable<T> source, 
            Func<T, IObservable<R>> mapper, 
            int maxConcurrency, 
            int capacityHint) : base(source)
        {
            this.mapper = mapper;
            this.maxConcurrency = maxConcurrency;
            this.capacityHint = capacityHint;
        }

        protected override BaseObserver<T, R> CreateObserver(IObserver<R> observer)
        {
            if (maxConcurrency == int.MaxValue)
            {
                return new ConcatMapEagerMaxObserver(observer, mapper, capacityHint);
            }

            return new ConcatMapEagerLimitedObserver(observer, mapper, capacityHint, maxConcurrency);
        }

        internal sealed class ConcatMapEagerMaxObserver : ConcatMapEagerObserver
        {
            internal ConcatMapEagerMaxObserver(IObserver<R> downstream, Func<T, IObservable<R>> mapper, int capacityHint) : base(downstream, mapper, capacityHint)
            {
            }

            public override void OnNext(T value)
            {
                UpstreamNext(value);
            }

            protected override void InnerConsumed()
            {
                // deliberately no-op
            }

            protected override void Cleanup()
            {
                // nothing extra to clean up
            }
        }

        internal sealed class ConcatMapEagerLimitedObserver : ConcatMapEagerObserver
        {
            readonly SpscLinkedArrayQueue<T> queue;

            long requested;

            long emitted;

            int backpressure;

            internal ConcatMapEagerLimitedObserver(IObserver<R> downstream, Func<T, IObservable<R>> mapper, int capacityHint, int maxConcurrency) : base(downstream, mapper, capacityHint)
            {
                Volatile.Write(ref requested, maxConcurrency);
                this.queue = new SpscLinkedArrayQueue<T>(capacityHint);
            }

            public override void OnNext(T value)
            {
                queue.Offer(value);
                DrainBackpressure();
            }

            protected override void InnerConsumed()
            {
                Interlocked.Increment(ref requested);
                DrainBackpressure();
            }

            protected override void Cleanup()
            {
                DrainBackpressure();
            }

            void DrainBackpressure()
            {
                if (Interlocked.Increment(ref backpressure) == 1)
                {
                    var missed = 1;
                    var e = emitted;
                    var q = queue;

                    for (; ; )
                    {
                        var r = Volatile.Read(ref requested);

                        while (e != r)
                        {
                            if (Volatile.Read(ref disposed))
                            {
                                q.Clear();
                                break;
                            }

                            if (!q.TryPoll(out var v))
                            {
                                break;
                            }

                            e++;
                            UpstreamNext(v);
                        }

                        if (e == r)
                        {
                            if (Volatile.Read(ref disposed))
                            {
                                q.Clear();
                                break;
                            }
                        }

                        emitted = e;
                        missed = Interlocked.Add(ref backpressure, -missed);
                        if (missed == 0)
                        {
                            break;
                        }
                    }
                }
            }
        }

        internal abstract class ConcatMapEagerObserver : BaseObserver<T, R>, IInnerObserverSupport<R>
        {
            readonly Func<T, IObservable<R>> mapper;
            readonly int capacityHint;

            readonly SpscLinkedArrayQueue<InnerObserver<R>> observers;

            int wip;

            Exception error;
            bool done;

            protected bool disposed;

            InnerObserver<R> current;

            internal ConcatMapEagerObserver(IObserver<R> downstream, Func<T, IObservable<R>> mapper,
                int capacityHint) : base(downstream)
            {
                this.mapper = mapper;
                this.capacityHint = capacityHint;
                this.observers = new SpscLinkedArrayQueue<InnerObserver<R>>(capacityHint);
            }

            public void InnerComplete(InnerObserver<R> sender)
            {
                sender.SetDone();
                Drain();
            }

            public void InnerError(InnerObserver<R> sender, Exception error)
            {
                if (Interlocked.CompareExchange(ref this.error, error, null) == null)
                {
                    sender.SetDone();
                    Volatile.Write(ref done, true);
                    Drain();
                }
            }

            public void InnerNext(InnerObserver<R> sender, R item)
            {
                if (Interlocked.CompareExchange(ref wip, 1, 0) == 0)
                {
                    var q = sender.GetQueue();

                    if (current == sender && (q == null || q.IsEmpty()))
                    {
                        downstream.OnNext(item);
                    }
                    else
                    {
                        if (q == null)
                        {
                            q = sender.CreateQueue(capacityHint);
                        }
                        q.Offer(item);
                    }
                    if (Interlocked.Decrement(ref wip) == 0)
                    {
                        return;
                    }
                }
                else
                {
                    var q = sender.GetOrCreateQueue(capacityHint);
                    q.Offer(item);
                    if (Interlocked.Increment(ref wip) != 1)
                    {
                        return;
                    }
                }

                DrainLoop();
            }

            public override void OnCompleted()
            {
                Volatile.Write(ref done, true);
                Drain();
            }

            public override void OnError(Exception error)
            {
                if (Interlocked.CompareExchange(ref this.error, error, null) == null)
                {
                    Volatile.Write(ref done, true);
                    Drain();
                }
            }

            
            public void UpstreamNext(T value)
            {
                if (IsDisposed())
                {
                    return;
                }
                var o = default(IObservable<R>);

                try
                {
                    o = mapper(value);
                }
                catch (Exception ex)
                {
                    base.Dispose();
                    OnError(ex);
                    return;
                }

                var inner = new InnerObserver<R>(this);

                observers.Offer(inner);

                if (!IsDisposed())
                {
                    inner.OnSubscribe(o.Subscribe(inner));
                }
                Drain();
            }

            public override void Dispose()
            {
                Volatile.Write(ref disposed, true);
                base.Dispose();
                Drain();
            }

            protected abstract void InnerConsumed();

            void Drain()
            {
                if (Interlocked.Increment(ref wip) == 1)
                {
                    DrainLoop();
                }
            }

            protected abstract void Cleanup();

            void DrainLoop()
            {
                int missed = 1;
                var observers = this.observers;

                for (; ; )
                {
                    if (Volatile.Read(ref disposed))
                    {
                        if (current != null)
                        {
                            current.Dispose();
                            current = null;
                        }

                        while (observers.TryPoll(out var inner))
                        {
                            inner.Dispose();
                        }
                        Cleanup();
                    } else
                    {
                        var curr = current;
                        if (curr == null)
                        {
                            var d = Volatile.Read(ref done);

                            if (d)
                            {
                                var ex = Volatile.Read(ref error);
                                if (ex != null)
                                {
                                    downstream.OnError(ex);
                                    Volatile.Write(ref disposed, true);
                                    base.Dispose();
                                    continue;
                                }
                            }

                            var empty = !observers.TryPoll(out curr);

                            if (d && empty)
                            {
                                downstream.OnCompleted();
                                Volatile.Write(ref disposed, true);
                                base.Dispose();
                            }

                            current = curr;
                        }
                        if (curr != null)
                        {
                            var continueOuter = false;
                            for (; ; )
                            {
                                if (Volatile.Read(ref disposed))
                                {
                                    continueOuter = true;
                                    break;
                                }

                                var d = curr.IsDone();
                                var q = curr.GetQueue();

                                var v = default(R);
                                var empty = q == null || !q.TryPoll(out v);

                                if (d && empty)
                                {
                                    curr.Dispose();
                                    current = null;
                                    curr = null;
                                    InnerConsumed();
                                    continueOuter = true;
                                    break;
                                }

                                if (empty)
                                {
                                    break;
                                }

                                downstream.OnNext(v);
                            }

                            if (continueOuter)
                            {
                                continue;
                            }
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
