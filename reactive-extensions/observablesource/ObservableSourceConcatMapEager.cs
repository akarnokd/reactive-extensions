using System;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    internal sealed class ObservableSourceConcatMapEager<T, R> : IObservableSource<R>
    {
        readonly IObservableSource<T> source;

        readonly Func<T, IObservableSource<R>> mapper;

        readonly int maxConcurrency;

        readonly int capacityHint;

        internal ObservableSourceConcatMapEager(
            IObservableSource<T> source, 
            Func<T, IObservableSource<R>> mapper, 
            int maxConcurrency, 
            int capacityHint)
        {
            this.source = source;
            this.mapper = mapper;
            this.maxConcurrency = maxConcurrency;
            this.capacityHint = capacityHint;
        }

        public void Subscribe(ISignalObserver<R> observer)
        {
            if (maxConcurrency == int.MaxValue)
            {
                source.Subscribe(new ConcatMapEagerMaxObserver(observer, mapper, capacityHint));
            }
            else
            {
                source.Subscribe(new ConcatMapEagerLimitedObserver(observer, mapper, capacityHint, maxConcurrency));
            }
        }


        internal sealed class ConcatMapEagerMaxObserver : ConcatMapEagerObserver
        {
            internal ConcatMapEagerMaxObserver(ISignalObserver<R> downstream, Func<T, IObservableSource<R>> mapper, int capacityHint) : base(downstream, mapper, capacityHint)
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

            internal ConcatMapEagerLimitedObserver(ISignalObserver<R> downstream, Func<T, IObservableSource<R>> mapper, int capacityHint, int maxConcurrency) : base(downstream, mapper, capacityHint)
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

                            var v = q.TryPoll(out var success);
                            if (!success)
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

        internal abstract class ConcatMapEagerObserver : BaseSignalObserver<T, R>, IInnerSignalObserverSupport<R>
        {
            readonly Func<T, IObservableSource<R>> mapper;
            readonly int capacityHint;

            readonly SpscLinkedArrayQueue<InnerSignalObserver<R>> observers;

            int wip;

            Exception error;
            bool done;

            protected bool disposed;

            InnerSignalObserver<R> current;

            internal ConcatMapEagerObserver(ISignalObserver<R> downstream, Func<T, IObservableSource<R>> mapper,
                int capacityHint) : base(downstream)
            {
                this.mapper = mapper;
                this.capacityHint = capacityHint;
                this.observers = new SpscLinkedArrayQueue<InnerSignalObserver<R>>(capacityHint);
            }

            public void InnerComplete(InnerSignalObserver<R> sender)
            {
                sender.SetDone();
                Drain();
            }

            public void InnerError(InnerSignalObserver<R> sender, Exception error)
            {
                if (Interlocked.CompareExchange(ref this.error, error, null) == null)
                {
                    sender.SetDone();
                    Volatile.Write(ref done, true);
                    Drain();
                }
            }

            public void InnerNext(InnerSignalObserver<R> sender, R item)
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
                        q.TryOffer(item);
                    }
                    if (Interlocked.Decrement(ref wip) == 0)
                    {
                        return;
                    }
                }
                else
                {
                    var q = sender.GetOrCreateQueue(capacityHint);
                    q.TryOffer(item);
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
                var o = default(IObservableSource<R>);

                try
                {
                    o = ValidationHelper.RequireNonNullRef(mapper(value), "The mapper returned a null IObservableSource");
                }
                catch (Exception ex)
                {
                    base.Dispose();
                    OnError(ex);
                    return;
                }

                var inner = new InnerSignalObserver<R>(this);

                observers.Offer(inner);

                if (!IsDisposed())
                {
                    o.Subscribe(inner);
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

            public void Drain()
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

                        for (; ; )
                        {
                            var inner = observers.TryPoll(out var success);
                            if (!success)
                            {
                                break;
                            }
                            inner.Dispose();
                        }
                        Cleanup();
                    }
                    else
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

                            curr = observers.TryPoll(out var success);
                            var empty = !success;

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
                                var success = false;

                                if (q != null)
                                {
                                    try
                                    {
                                        v = q.TryPoll(out success);
                                    }
                                    catch (Exception ex)
                                    {
                                        Interlocked.CompareExchange(ref this.error, ex, null);
                                        Volatile.Write(ref disposed, true);
                                        continueOuter = true;
                                        break;
                                    }
                                }

                                if (d && !success)
                                {
                                    curr.Dispose();
                                    current = null;
                                    curr = null;
                                    InnerConsumed();
                                    continueOuter = true;
                                    break;
                                }

                                if (!success)
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
