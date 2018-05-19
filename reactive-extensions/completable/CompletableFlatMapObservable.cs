using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using static akarnokd.reactive_extensions.ValidationHelper;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Maps the upstream observable items into completable sources
    /// and runs some or all of them until all terminate.
    /// </summary>
    /// <remarks>Since 0.0.10</remarks>
    internal sealed class CompletableFlatMapObservable<T> : ICompletableSource
    {
        readonly IObservable<T> source;

        readonly Func<T, ICompletableSource> mapper;

        readonly bool delayErrors;

        readonly int maxConcurrency;

        public CompletableFlatMapObservable(IObservable<T> source, Func<T, ICompletableSource> mapper, bool delayErrors, int maxConcurrency)
        {
            this.source = source;
            this.mapper = mapper;
            this.delayErrors = delayErrors;
            this.maxConcurrency = maxConcurrency;
        }

        public void Subscribe(ICompletableObserver observer)
        {
            if (maxConcurrency != int.MaxValue)
            {
                var parent = new FlatMapLimitedObserver(observer, mapper, delayErrors, maxConcurrency);
                observer.OnSubscribe(parent);

                parent.OnSubscribe(source.Subscribe(parent));
            }
            else
            {
                var parent = new FlatMapAllObserver(observer, mapper, delayErrors);
                observer.OnSubscribe(parent);

                parent.OnSubscribe(source.Subscribe(parent));
            }
        }

        internal abstract class FlatMapObserver : IObserver<T>, IDisposable
        {
            protected readonly ICompletableObserver downstream;

            protected readonly Func<T, ICompletableSource> mapper;

            protected readonly bool delayErrors;

            protected HashSet<IDisposable> observers;

            protected IDisposable upstream;

            protected bool disposed;

            protected Exception errors;
            protected volatile bool done;

            protected int active;

            public FlatMapObserver(ICompletableObserver downstream, Func<T, ICompletableSource> mapper, bool delayErrors)
            {
                this.downstream = downstream;
                this.mapper = mapper;
                this.delayErrors = delayErrors;
                this.observers = new HashSet<IDisposable>();
            }

            public virtual void Dispose()
            {
                DisposableHelper.Dispose(ref upstream);
                DisposeAll();
            }

            protected void DisposeAll()
            {
                if (Volatile.Read(ref disposed))
                {
                    return;
                }
                var o = default(HashSet<IDisposable>);

                lock (this)
                {
                    if (Volatile.Read(ref disposed))
                    {
                        return;
                    }
                    o = observers;
                    observers = null;
                    Volatile.Write(ref disposed, true);
                }

                foreach (var inner in o)
                {
                    inner.Dispose();
                }
            }

            protected bool Add(InnerObserver inner)
            {
                if (Volatile.Read(ref disposed))
                {
                    return false;
                }

                lock (this)
                {
                    if (Volatile.Read(ref disposed))
                    {
                        return false;
                    }
                    observers.Add(inner);
                    return true;
                }
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
                    observers.Remove(inner);
                }
            }

            public void OnSubscribe(IDisposable d)
            {
                DisposableHelper.SetOnce(ref upstream, d);
            }

            void InnerComplete(InnerObserver sender)
            {
                Remove(sender);
                Interlocked.Decrement(ref active);
                Drain();
            }

            void InnerError(InnerObserver sender, Exception ex)
            {
                Remove(sender);
                if (delayErrors)
                {
                    ExceptionHelper.AddException(ref errors, ex);
                }
                else
                {
                    Interlocked.CompareExchange(ref errors, ex, null);
                }
                Interlocked.Decrement(ref active);
                Drain();
            }

            protected abstract void Drain();

            public void OnCompleted()
            {
                if (done)
                {
                    return;
                }
                done = true;
                Drain();
            }

            public void OnError(Exception error)
            {
                if (done)
                {
                    return;
                }
                if (delayErrors)
                {
                    ExceptionHelper.AddException(ref errors, error);
                }
                else
                {
                    Interlocked.CompareExchange(ref errors, error, null);
                }
                done = true;
                Drain();
            }

            public abstract void OnNext(T value);

            internal sealed class InnerObserver : ICompletableObserver, IDisposable
            {
                readonly FlatMapObserver parent;

                IDisposable upstream;

                public InnerObserver(FlatMapObserver parent)
                {
                    this.parent = parent;
                }

                public void Dispose()
                {
                    DisposableHelper.Dispose(ref upstream);
                }

                public void OnCompleted()
                {
                    parent.InnerComplete(this);
                }

                public void OnError(Exception error)
                {
                    parent.InnerError(this, error);
                }

                public void OnSubscribe(IDisposable d)
                {
                    DisposableHelper.SetOnce(ref upstream, d);
                }
            }
        }

        internal sealed class FlatMapLimitedObserver : FlatMapObserver
        {

            readonly int maxConcurrency;

            readonly ConcurrentQueue<T> queue;

            int wip;

            bool mapperCrash;

            public FlatMapLimitedObserver(ICompletableObserver downstream, Func<T, ICompletableSource> mapper, bool delayErrors, int maxConcurrency) : base(downstream, mapper, delayErrors)
            {
                this.maxConcurrency = maxConcurrency;
                this.queue = new ConcurrentQueue<T>();
            }

            public override void OnNext(T value)
            {
                queue.Enqueue(value);
                Drain();
            }

            public override void Dispose()
            {
                base.Dispose();
                Drain();
            }

            protected override void Drain()
            {
                if (Interlocked.Increment(ref wip) != 1)
                {
                    return;
                }

                var missed = 1;
                var q = queue;
                var maxConcurrency = this.maxConcurrency;

                for (; ; )
                {
                    if (Volatile.Read(ref disposed))
                    {
                        while (q.TryDequeue(out var _)) ;
                    }
                    else
                    {
                        if (!delayErrors)
                        {
                            var ex = Volatile.Read(ref errors);
                            if (ex != null)
                            {
                                downstream.OnError(ex);

                                base.Dispose();
                                continue;
                            }
                        }

                        if (mapperCrash)
                        {
                            while (q.TryDequeue(out var _)) ;
                        }
                        else
                        if (Volatile.Read(ref active) < maxConcurrency && !q.IsEmpty)
                        {
                            if (q.TryDequeue(out var v))
                            {
                                var c = default(ICompletableSource);
                                try
                                {
                                    c = RequireNonNullRef(mapper(v), "The mapper returned a null ICompletableSource");
                                }
                                catch (Exception ex)
                                {
                                    mapperCrash = true;
                                    if (delayErrors)
                                    {
                                        ExceptionHelper.AddException(ref errors, ex);
                                    }
                                    else
                                    {
                                        Interlocked.CompareExchange(ref errors, ex, null);
                                    }
                                    done = true;
                                    DisposableHelper.Dispose(ref upstream);
                                    continue;
                                }

                                var inner = new InnerObserver(this);
                                if (Add(inner))
                                {
                                    Interlocked.Increment(ref active);
                                    c.Subscribe(inner);
                                }
                                continue;
                            }
                        }

                        var d = done;
                        var a = Volatile.Read(ref active);
                        var empty = a == 0 &&q.IsEmpty;

                        if (d && empty)
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
                            base.Dispose();
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

        internal sealed class FlatMapAllObserver : FlatMapObserver
        {

            public FlatMapAllObserver(ICompletableObserver downstream, Func<T, ICompletableSource> mapper, bool delayErrors) : base(downstream, mapper, delayErrors)
            {
            }

            public override void OnNext(T value)
            {
                if (done)
                {
                    return;
                }

                var c = default(ICompletableSource);

                try
                {
                    c = RequireNonNullRef(mapper(value), "The mapper returned a null ICompletableSource");
                }
                catch (Exception ex)
                {
                    OnError(ex);
                    DisposableHelper.Dispose(ref upstream);
                    return;
                }

                var inner = new InnerObserver(this);
                if (Add(inner))
                {
                    Interlocked.Increment(ref active);
                    c.Subscribe(inner);
                }
            }

            protected override void Drain()
            {
                var d = done;
                var a = Volatile.Read(ref active);

                if (delayErrors)
                {
                    if (d && a == 0)
                    {
                        if (Interlocked.Exchange(ref active, -1) >= 0)
                        {
                            var ex = Volatile.Read(ref errors);
                            if (ex != null)
                            {
                                downstream.OnError(ex);
                            }
                            else
                            {
                                downstream.Complete();
                            }

                            Volatile.Write(ref disposed, true);
                            DisposableHelper.Dispose(ref upstream);
                        }
                    }
                }
                else
                {
                    var ex = Volatile.Read(ref errors);
                    if (ex != null)
                    {
                        if (Interlocked.Exchange(ref active, -1) >= 0)
                        {
                            downstream.OnError(ex);
                            Dispose();
                        }
                    }
                    else
                    if (d && a == 0)
                    {
                        if (Interlocked.Exchange(ref active, -1) >= 0)
                        {
                            downstream.Complete();
                            Volatile.Write(ref disposed, true);
                            DisposableHelper.Dispose(ref upstream);
                        }
                    }
                }
            }
        }
    }
}
