using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Merges some or all observables provided via an outer observable.
    /// </summary>
    /// <typeparam name="T">The result element type.</typeparam>
    internal sealed class MergeMany<T> : BaseObservable<IObservable<T>, T>
    {
        readonly bool delayErrors;
        readonly int maxConcurrency;
        readonly int capacityHint;

        internal MergeMany(IObservable<IObservable<T>> source, bool delayErrors, int maxConcurrency, int capacityHint) : base(source)
        {
            this.delayErrors = delayErrors;
            this.maxConcurrency = maxConcurrency;
            this.capacityHint = capacityHint;
        }

        protected override BaseObserver<IObservable<T>, T> CreateObserver(IObserver<T> observer)
        {
            if (maxConcurrency == int.MaxValue)
            {
                return new MergeManyMaxObserver(observer, delayErrors, capacityHint);
            }
            return new MergeManyLimitedObserver(observer, delayErrors, maxConcurrency, capacityHint);
        }

        internal abstract class MergeManyBaseObserver : BaseObserver<IObservable<T>, T>
        {
            protected readonly bool delayErrors;
            protected readonly int capacityHint;

            protected HashSet<IDisposable> disposables;

            protected ConcurrentQueue<T> queue;

            protected int wip;

            protected Exception errors;

            protected bool disposed;

            protected int active;

            internal MergeManyBaseObserver(IObserver<T> downstream, bool delayErrors, int capacityHint) : base(downstream)
            {
                this.delayErrors = delayErrors;
                this.capacityHint = capacityHint;
                this.disposables = new HashSet<IDisposable>();
            }

            protected void UpstreamNext(IObservable<T> value)
            {
                Interlocked.Increment(ref active);
                var inner = new InnerObserver(this);
                if (Add(inner))
                {
                    inner.OnSubscribe(value.Subscribe(inner));
                }

            }

            protected bool Add(InnerObserver inner)
            {
                if (!Volatile.Read(ref disposed))
                {
                    lock (this)
                    {
                        if (!Volatile.Read(ref disposed))
                        {
                            disposables.Add(inner);
                            return true;
                        }
                    }
                }
                return false;
            }

            protected void Remove(InnerObserver inner)
            {
                if (Volatile.Read(ref disposed))
                {
                    return;
                }
                lock (this)
                {
                    if (Volatile.Read(ref disposed))
                    {
                        return;
                    }
                    disposables.Remove(inner);
                }
                inner.Dispose();
            }

            protected void DisposeAll()
            {
                if (Volatile.Read(ref disposed))
                {
                    return;
                }
                var all = default(HashSet<IDisposable>);
                lock (this)
                {
                    if (Volatile.Read(ref disposed))
                    {
                        return;
                    }
                    all = disposables;
                    disposables = null;
                }

                foreach (var d in all)
                {
                    d?.Dispose();
                }
            }

            public override void Dispose()
            {
                Volatile.Write(ref disposed, true);
                base.Dispose();
                DisposeAll();
                Drain();
            }

            protected void Drain()
            {
                if (Interlocked.Increment(ref wip) == 1)
                {
                    DrainLoop();
                }
            }

            protected abstract void DrainLoop();

            protected ConcurrentQueue<T> GetQueue()
            {
                return Volatile.Read(ref queue);
            }

            protected ConcurrentQueue<T> GetOrCreateQueue()
            {
                var q = Volatile.Read(ref queue);
                if (q == null)
                {
                    q = new ConcurrentQueue<T>();
                    if (Interlocked.CompareExchange(ref queue, q, null) != null)
                    {
                        q = Volatile.Read(ref queue);
                    }
                }
                return q;
            }

            void InnerNext(T item)
            {
                if (Interlocked.CompareExchange(ref wip, 1, 0) == 0)
                {
                    var q = GetQueue();
                    if (q == null || q.IsEmpty)
                    {
                        downstream.OnNext(item);

                        if (Interlocked.Decrement(ref wip) == 0)
                        {
                            return;
                        }
                    }
                    else
                    {
                        q.Enqueue(item);
                    }
                }
                else
                {
                    var q = GetOrCreateQueue();
                    q.Enqueue(item);
                    if (Interlocked.Increment(ref wip) != 1)
                    {
                        return;
                    }
                }
                DrainLoop();
            }

            protected abstract void InnerError(Exception ex, InnerObserver sender);

            protected abstract void InnerComplete(InnerObserver sender);

            internal sealed class InnerObserver : IObserver<T>, IDisposable
            {
                readonly MergeManyBaseObserver parent;

                IDisposable upstream;

                public InnerObserver(MergeManyBaseObserver parent)
                {
                    this.parent = parent;
                }

                public void Dispose()
                {
                    DisposableHelper.Dispose(ref upstream);
                }

                public void OnSubscribe(IDisposable d)
                {
                    DisposableHelper.SetOnce(ref upstream, d);
                }

                public void OnCompleted()
                {
                    parent.InnerComplete(this);
                }

                public void OnError(Exception error)
                {
                    parent.InnerError(error, this);
                }

                public void OnNext(T value)
                {
                    parent.InnerNext(value);
                }
            }
        }

        internal sealed class MergeManyLimitedObserver : MergeManyBaseObserver
        {
            readonly ConcurrentQueue<IObservable<T>> sources;

            readonly int maxConcurrency;

            bool done;

            internal MergeManyLimitedObserver(IObserver<T> downstream, bool delayErrors, int maxConcurrency, int capacityHint) : base(downstream, delayErrors, capacityHint)
            {
                this.maxConcurrency = maxConcurrency;
                this.sources = new ConcurrentQueue<IObservable<T>>();
            }

            public override void OnCompleted()
            {
                Volatile.Write(ref done, true);
                Drain();
            }

            public override void OnError(Exception error)
            {
                if (delayErrors)
                {
                    ExceptionHelper.AddException(ref this.errors, error);
                    Volatile.Write(ref done, true);
                    if (Volatile.Read(ref active) == 0)
                    {
                        Drain();
                    }
                }
                else
                {
                    if (Interlocked.CompareExchange(ref this.errors, error, null) == null)
                    {
                        Drain();
                    }
                }
            }

            public override void OnNext(IObservable<T> value)
            {
                sources.Enqueue(value);
                Drain();
            }

            protected override void DrainLoop()
            {
                var missed = 1;
                var downstream = this.downstream;
                var delayErrors = this.delayErrors;
                var sources = this.sources;

                for (; ; )
                {
                    if (Volatile.Read(ref disposed))
                    {
                        var q = GetQueue();
                        if (q != null)
                        {
                            while (q.TryDequeue(out var _)) ;
                        }

                        while (sources.TryDequeue(out var _)) ;
                    }
                    else
                    {
                        var continueOuter = false;
                        var r = maxConcurrency;
                        
                        while (Volatile.Read(ref active) < r)
                        {
                            if (Volatile.Read(ref disposed))
                            {
                                continueOuter = true;
                                break;
                            }

                            if (sources.TryDequeue(out var src))
                            {
                                UpstreamNext(src);
                            }
                            else
                            {
                                break;
                            }
                        }

                        if (continueOuter)
                        {
                            continue;
                        }

                        continueOuter = false;

                        var act = Volatile.Read(ref active);

                        for (; ; )
                        {
                            if (Volatile.Read(ref disposed))
                            {
                                continueOuter = true;
                                break;
                            }

                            if (!delayErrors)
                            {
                                var ex = Volatile.Read(ref errors);
                                if (ex != null)
                                {
                                    Volatile.Write(ref disposed, true);
                                    downstream.OnError(ex);
                                    base.Dispose();
                                    DisposeAll();
                                    continueOuter = true;
                                    break;
                                }
                            }

                            bool d = Volatile.Read(ref done) && Volatile.Read(ref active) == 0;
                            var q = GetQueue();
                            var v = default(T);
                            bool empty = q == null || !q.TryDequeue(out v);

                            if (d && empty && sources.IsEmpty)
                            {
                                var ex = Volatile.Read(ref errors);
                                if (ex != null)
                                {
                                    downstream.OnError(ex);
                                }
                                else
                                {
                                    downstream.OnCompleted();
                                }
                                Volatile.Write(ref disposed, true);
                                base.Dispose();
                                DisposeAll();
                                break;
                            }

                            if (empty)
                            {
                                break;
                            }

                            downstream.OnNext(v);

                            if (act != Volatile.Read(ref active))
                            {
                                continueOuter = true;
                                break;
                            }
                        }

                        if (continueOuter)
                        {
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

            protected override void InnerComplete(InnerObserver sender)
            {
                Remove(sender);
                Interlocked.Decrement(ref active);
                Drain();
            }

            protected override void InnerError(Exception ex, InnerObserver sender)
            {
                Remove(sender);
                if (delayErrors)
                {
                    ExceptionHelper.AddException(ref this.errors, ex);
                    Interlocked.Decrement(ref active);
                    Drain();
                }
                else
                {
                    if (Interlocked.CompareExchange(ref this.errors, ex, null) == null)
                    {
                        Drain();
                    }
                }
            }
        }

        internal sealed class MergeManyMaxObserver : MergeManyBaseObserver
        {

            internal MergeManyMaxObserver(IObserver<T> downstream, bool delayErrors, int capacityHint) : base(downstream, delayErrors, capacityHint)
            {
                Volatile.Write(ref active, 1);
            }

            public override void OnCompleted()
            {
                if (Interlocked.Decrement(ref active) == 0)
                {
                    Drain();
                }
            }

            public override void OnError(Exception error)
            {
                if (delayErrors)
                {
                    ExceptionHelper.AddException(ref this.errors, error);
                    if (Interlocked.Decrement(ref active) == 0)
                    {
                        Drain();
                    }
                }
                else
                {
                    if (Interlocked.CompareExchange(ref this.errors, error, null) == null)
                    {
                        Drain();
                    }
                }
            }

            public override void OnNext(IObservable<T> value)
            {
                UpstreamNext(value);
            }

            protected override void DrainLoop()
            {
                int missed = 1;
                var downstream = this.downstream;
                var delayErrors = this.delayErrors;


                for (; ; )
                {
                    for (; ; )
                    {
                        if (Volatile.Read(ref disposed))
                        {
                            var q = GetQueue();
                            if (q != null)
                            {
                                while (q.TryDequeue(out var _)) ;
                            }

                            break;
                        }
                        else
                        {
                            if (!delayErrors)
                            {
                                var ex = Volatile.Read(ref errors);
                                if (ex != null)
                                {
                                    Volatile.Write(ref disposed, true);
                                    downstream.OnError(ex);
                                    base.Dispose();
                                    DisposeAll();
                                    continue;
                                }
                            }

                            bool d = Volatile.Read(ref active) == 0;
                            var q = GetQueue();
                            var v = default(T);
                            bool empty = q == null || !q.TryDequeue(out v);

                            if (d && empty)
                            {
                                var ex = Volatile.Read(ref errors);
                                if (ex != null)
                                {
                                    downstream.OnError(ex);
                                } else
                                {
                                    downstream.OnCompleted();
                                }
                                Volatile.Write(ref disposed, true);
                                base.Dispose();
                                DisposeAll();
                                continue;
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


            protected override void InnerError(Exception ex, InnerObserver sender)
            {
                Remove(sender);
                if (delayErrors)
                {
                    ExceptionHelper.AddException(ref this.errors, ex);
                    if (Interlocked.Decrement(ref active) == 0)
                    {
                        Drain();
                    }
                }
                else
                {
                    if (Interlocked.CompareExchange(ref this.errors, ex, null) == null)
                    {
                        Drain();
                    }
                }
            }

            protected override void InnerComplete(InnerObserver sender)
            {
                Remove(sender);
                if (Interlocked.Decrement(ref active) == 0)
                {
                    Drain();
                }
            }
        }
    }
}
