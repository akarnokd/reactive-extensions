using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using static akarnokd.reactive_extensions.ValidationHelper;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Merges an array of completable sources and completes if
    /// all of the sources complete or terminate.
    /// </summary>
    /// <remarks>Since 0.0.10</remarks>
    internal sealed class CompletableMerge : ICompletableSource
    {
        readonly ICompletableSource[] sources;

        readonly bool delayErrors;

        readonly int maxConcurrency;

        public CompletableMerge(ICompletableSource[] sources, bool delayErrors, int maxConcurrency)
        {
            this.sources = sources;
            this.delayErrors = delayErrors;
            this.maxConcurrency = maxConcurrency;
        }

        public void Subscribe(ICompletableObserver observer)
        {
            if (maxConcurrency == int.MaxValue)
            {
                var parent = new MergeAllCoordinator(observer, delayErrors);
                observer.OnSubscribe(parent);
                parent.SubscribeAll(sources);
            }
            else
            {
                var parent = new MergeLimitedCoordinator(observer, sources, delayErrors, maxConcurrency);
                observer.OnSubscribe(parent);
                parent.Drain();
            }
        }

        sealed class MergeLimitedCoordinator : CompletableMergeCoordinator
        {
            readonly ICompletableSource[] sources;

            readonly int maxConcurrency;

            int index;

            int wip;

            internal MergeLimitedCoordinator(ICompletableObserver downstream, ICompletableSource[] sources, bool delayErrors, int maxConcurrency) : base(downstream, delayErrors)
            {
                this.sources = sources;
                this.maxConcurrency = maxConcurrency;
            }

            internal override void Drain()
            {
                if (Interlocked.Increment(ref wip) != 1)
                {
                    return;
                }

                var missed = 1;
                var sources = this.sources;
                var n = sources.Length;
                var maxConcurrency = this.maxConcurrency;

                for (; ; )
                {
                    if (!Volatile.Read(ref disposed))
                    {
                        if (!delayErrors)
                        {
                            var ex = Volatile.Read(ref errors);
                            if (ex != null)
                            {
                                base.Dispose();
                                downstream.OnError(ex);
                                continue;
                            }
                        }

                        if (index < n && Volatile.Read(ref active) < maxConcurrency)
                        {
                            var src = sources[index++];

                            if (src == null)
                            {
                                var ex = new NullReferenceException("The ICompletableSource at index " + (index - 1) + " is null");
                                if (delayErrors)
                                {
                                    index = n;
                                    ExceptionHelper.AddException(ref errors, ex);
                                }
                                else
                                {
                                    Interlocked.CompareExchange(ref errors, ex, null);
                                }
                                continue;
                            }

                            var inner = new MergeInnerObserver(this);
                            if (Add(inner))
                            {
                                Interlocked.Increment(ref active);
                                src.Subscribe(inner);
                            }
                            continue;
                        }

                        var d = Volatile.Read(ref active) == 0;
                        var empty = index == n;

                        if (d && empty)
                        {
                            var ex = Volatile.Read(ref errors);
                            if (ex != null)
                            {
                                Volatile.Write(ref disposed, true);
                                downstream.OnError(ex);
                            }
                            else
                            {
                                Volatile.Write(ref disposed, true);
                                downstream.OnCompleted();
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

        sealed class MergeAllCoordinator : CompletableMergeCoordinator
        {
            internal MergeAllCoordinator(ICompletableObserver downstream, bool delayErrors) : base(downstream, delayErrors)
            {
            }

            internal void SubscribeAll(ICompletableSource[] sources)
            {
                Interlocked.Exchange(ref active, sources.Length);
                foreach (var src in sources)
                {
                    var inner = new MergeInnerObserver(this);
                    if (!Add(inner))
                    {
                        break;
                    }
                    src.Subscribe(inner);
                }
            }

            internal override void Drain()
            {
                if (delayErrors)
                {
                    var a = Volatile.Read(ref active);
                    var ex = Volatile.Read(ref errors);

                    if (a == 0 && Interlocked.CompareExchange(ref active, -1, 0) == 0)
                    {
                        if (ex != null)
                        {
                            downstream.OnError(ex);
                        }
                        else
                        {
                            downstream.OnCompleted();
                        }
                    }
                }
                else
                {
                    var a = Volatile.Read(ref active);
                    var ex = Volatile.Read(ref errors);

                    if (ex != null)
                    {
                        if (a != -1 && Interlocked.Exchange(ref active, -1) != -1)
                        {
                            Dispose();
                            downstream.OnError(ex);
                        }
                    }
                    else
                    {
                        if (a == 0 && Interlocked.CompareExchange(ref active, -1, 0) == 0)
                        {
                            downstream.OnCompleted();
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Base class for supporting the merge operator with
    /// inner completable observers and some termination handling.
    /// </summary>
    /// <remarks>Since 0.0.10</remarks>
    internal abstract class CompletableMergeCoordinator : IDisposable
    {
        protected readonly ICompletableObserver downstream;

        protected readonly bool delayErrors;

        protected Exception errors;

        HashSet<IDisposable> observers;

        protected bool disposed;

        protected int active;

        protected CompletableMergeCoordinator(ICompletableObserver downstream, bool delayErrors)
        {
            this.downstream = downstream;
            this.delayErrors = delayErrors;
            this.observers = new HashSet<IDisposable>();
        }

        public virtual void Dispose()
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

            foreach (var d in o)
            {
                d.Dispose();
            }
        }

        void Remove(MergeInnerObserver inner)
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

        protected bool Add(MergeInnerObserver inner)
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
            }
            return true;
        }

        void InnerCompleted(MergeInnerObserver sender)
        {
            Remove(sender);
            Interlocked.Decrement(ref active);
            Drain();
        }

        void InnerError(MergeInnerObserver sender, Exception ex)
        {
            Remove(sender);
            if (delayErrors)
            {
                ExceptionHelper.AddException(ref errors, ex);
                Interlocked.Decrement(ref active);
                Drain();
            }
            else
            {
                if (Interlocked.CompareExchange(ref errors, ex, null) == null)
                {
                    Drain();
                }
            }
        }

        internal abstract void Drain();

        internal sealed class MergeInnerObserver : ICompletableObserver, IDisposable
        {
            readonly CompletableMergeCoordinator parent;

            IDisposable upstream;

            public MergeInnerObserver(CompletableMergeCoordinator parent)
            {
                this.parent = parent;
            }

            public void Dispose()
            {
                DisposableHelper.Dispose(ref upstream);
            }

            public void OnCompleted()
            {
                parent.InnerCompleted(this);
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

    /// <summary>
    /// Merges an enumerable sequence of completable sources and completes if
    /// all of the sources complete or terminate.
    /// </summary>
    /// <remarks>Since 0.0.10</remarks>
    internal sealed class CompletableMergeEnumerable : ICompletableSource
    {
        readonly IEnumerable<ICompletableSource> sources;

        readonly bool delayErrors;

        readonly int maxConcurrency;

        public CompletableMergeEnumerable(IEnumerable<ICompletableSource> sources, bool delayErrors, int maxConcurrency)
        {
            this.sources = sources;
            this.delayErrors = delayErrors;
            this.maxConcurrency = maxConcurrency;
        }

        public void Subscribe(ICompletableObserver observer)
        {
            if (maxConcurrency == int.MaxValue)
            {
                var parent = new MergeAllCoordinator(observer, delayErrors);
                observer.OnSubscribe(parent);
                parent.SubscribeAll(sources);
            }
            else
            {
                var en = default(IEnumerator<ICompletableSource>);
                try
                {
                    en = sources.GetEnumerator();
                }
                catch (Exception ex)
                {
                    DisposableHelper.Error(observer, ex);
                    return;
                }

                var parent = new MergeLimitedCoordinator(observer, en, delayErrors, maxConcurrency);
                observer.OnSubscribe(parent);
                parent.Drain();
            }
        }

        sealed class MergeLimitedCoordinator : CompletableMergeCoordinator
        {
            readonly IEnumerator<ICompletableSource> sources;

            readonly int maxConcurrency;

            int wip;

            bool done;

            internal MergeLimitedCoordinator(ICompletableObserver downstream, IEnumerator<ICompletableSource> sources, bool delayErrors, int maxConcurrency) : base(downstream, delayErrors)
            {
                this.sources = sources;
                this.maxConcurrency = maxConcurrency;
            }

            public override void Dispose()
            {
                base.Dispose();
                Drain();
            }

            internal override void Drain()
            {
                if (Interlocked.Increment(ref wip) != 1)
                {
                    return;
                }

                var missed = 1;
                var sources = this.sources;
                var maxConcurrency = this.maxConcurrency;

                for (; ; )
                {
                    if (Volatile.Read(ref disposed))
                    {
                        if (!done)
                        {
                            done = true;
                            sources.Dispose();
                        }
                    }
                    else
                    {
                        if (!delayErrors)
                        {
                            var ex = Volatile.Read(ref errors);
                            if (ex != null)
                            {
                                base.Dispose();
                                downstream.OnError(ex);
                                continue;
                            }
                        }

                        if (!done && Volatile.Read(ref active) < maxConcurrency)
                        {

                            var src = default(ICompletableSource);
                            var empty = true;
                            try
                            {
                                if (sources.MoveNext())
                                {
                                    empty = false;
                                    src = RequireNonNullRef(sources.Current, "The IEnumerator returned a null ICompletableSource");
                                }
                            } catch (Exception ex)
                            {
                                if (delayErrors)
                                {
                                    ExceptionHelper.AddException(ref errors, ex);
                                }
                                else
                                {
                                    Interlocked.CompareExchange(ref errors, ex, null);
                                    continue;
                                }
                            }

                            if (empty)
                            {
                                done = true;
                                sources.Dispose();
                            }
                            else
                            {
                                var inner = new MergeInnerObserver(this);
                                if (Add(inner))
                                {
                                    Interlocked.Increment(ref active);
                                    src.Subscribe(inner);
                                }
                                continue;
                            }
                        }

                        var d = Volatile.Read(ref active) == 0;

                        if (d && done)
                        {
                            var ex = Volatile.Read(ref errors);
                            if (ex != null)
                            {
                                Volatile.Write(ref disposed, true);
                                downstream.OnError(ex);
                            }
                            else
                            {
                                Volatile.Write(ref disposed, true);
                                downstream.OnCompleted();
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

        sealed class MergeAllCoordinator : CompletableMergeCoordinator
        {
            internal MergeAllCoordinator(ICompletableObserver downstream, bool delayErrors) : base(downstream, delayErrors)
            {
            }

            internal void SubscribeAll(IEnumerable<ICompletableSource> sources)
            {
                Interlocked.Increment(ref active);
                try
                {
                    foreach (var src in sources)
                    {
                        var inner = new MergeInnerObserver(this);
                        if (!Add(inner))
                        {
                            break;
                        }
                        Interlocked.Increment(ref active);
                        src.Subscribe(inner);
                    }
                }
                catch (Exception ex)
                {
                    if (delayErrors)
                    {
                        ExceptionHelper.AddException(ref errors, ex);
                    }
                    else
                    {
                        Interlocked.CompareExchange(ref errors, ex, null);
                    }
                }
                Interlocked.Decrement(ref active);
                Drain();
            }

            internal override void Drain()
            {
                if (delayErrors)
                {
                    var a = Volatile.Read(ref active);
                    var ex = Volatile.Read(ref errors);

                    if (a == 0 && Interlocked.CompareExchange(ref active, -1, 0) == 0)
                    {
                        if (ex != null)
                        {
                            downstream.OnError(ex);
                        }
                        else
                        {
                            downstream.OnCompleted();
                        }
                    }
                }
                else
                {
                    var a = Volatile.Read(ref active);
                    var ex = Volatile.Read(ref errors);

                    if (ex != null)
                    {
                        if (a >= 0 && Interlocked.Exchange(ref active, -1) >= 0)
                        {
                            Dispose();
                            downstream.OnError(ex);
                        }
                    }
                    else
                    {
                        if (a == 0 && Interlocked.CompareExchange(ref active, -1, 0) == 0)
                        {
                            downstream.OnCompleted();
                        }
                    }
                }
            }
        }

    }
}
