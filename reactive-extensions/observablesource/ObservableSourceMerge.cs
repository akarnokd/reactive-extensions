using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using static akarnokd.reactive_extensions.ValidationHelper;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Merges some or all observables provided via an array.
    /// </summary>
    /// <typeparam name="T">The result element type.</typeparam>
    /// <remarks>Since 0.0.23</remarks>
    internal sealed class ObservableSourceMergeArray<T> : IObservableSource<T>
    {
        readonly IObservableSource<T>[] sources;

        readonly bool delayErrors;
        readonly int maxConcurrency;

        internal ObservableSourceMergeArray(IObservableSource<T>[] sources, bool delayErrors, int maxConcurrency)
        {
            this.sources = sources;
            this.delayErrors = delayErrors;
            this.maxConcurrency = maxConcurrency;
        }

        public void Subscribe(ISignalObserver<T> observer)
        {
            if (maxConcurrency == int.MaxValue)
            {
                var parent = new MergeMaxCoordinator(observer, delayErrors);
                observer.OnSubscribe(parent);

                foreach (var source in sources)
                {
                    if (source == null)
                    {
                        parent.Error(new NullReferenceException());
                        return;
                    }
                    if (!parent.SubscribeTo(source))
                    {
                        return;
                    }
                }

                parent.SetDone();
            }
            else
            {
                var parent = new MergeLimitedCoordinator(observer, delayErrors, maxConcurrency, sources);
                observer.OnSubscribe(parent);

                parent.Drain();
            }
        }

        sealed class MergeLimitedCoordinator : ObservableSourceMergeBase<T>
        {
            readonly IObservableSource<T>[] array;

            readonly int maxConcurrency;

            int index;

            internal MergeLimitedCoordinator(ISignalObserver<T> downstream, bool delayErrors, int maxConcurrency, IObservableSource<T>[] array) : base(downstream, delayErrors)
            {
                this.maxConcurrency = maxConcurrency;
                this.array = array;
            }

            protected override void DrainLoop()
            {
                var missed = 1;
                var delayErrors = this.delayErrors;
                var downstream = this.downstream;
                var array = this.array;

                for (; ; )
                {
                    if (Volatile.Read(ref disposed))
                    {
                        var q = GetQueue();
                        if (q != null)
                        {
                            while (q.TryDequeue(out var _)) ;
                        }
                    }
                    else
                    {
                        if (!delayErrors)
                        {
                            var ex = Volatile.Read(ref errors);
                            if (ex != null)
                            {
                                downstream.OnError(ex);
                                DisposeAll();
                                continue;
                            }
                        }

                        var a = Volatile.Read(ref active);

                        var idx = index;
                        if (idx != array.Length && a < maxConcurrency)
                        {
                            var src = array[idx];
                            if (src == null)
                            {
                                var ex = new NullReferenceException($"The sources[{idx}] is null");
                                if (delayErrors)
                                {
                                    ExceptionHelper.AddException(ref errors, ex);
                                }
                                else
                                {
                                    Interlocked.CompareExchange(ref errors, ex, null);
                                }
                                index = array.Length;
                            }
                            else
                            {
                                SubscribeTo(src);
                                index = idx + 1;
                            }
                            continue;
                        }

                        var q = GetQueue();

                        var v = default(T);

                        var success = q != null && q.TryDequeue(out v);

                        if (a == 0 && !success)
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
                        }
                        else
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
        }

        sealed class MergeMaxCoordinator : ObservableSourceMergeBase<T>
        {
            internal MergeMaxCoordinator(ISignalObserver<T> downstream, bool delayErrors) : base(downstream, delayErrors)
            {
            }

            protected override void DrainLoop()
            {
                var missed = 1;
                var delayErrors = this.delayErrors;
                var downstream = this.downstream;

                for (; ; )
                {
                    if (Volatile.Read(ref disposed))
                    {
                        var q = GetQueue();
                        if (q != null)
                        {
                            while (q.TryDequeue(out var _)) ;
                        }
                    }
                    else
                    {
                        if (!delayErrors)
                        {
                            var ex = Volatile.Read(ref errors);
                            if (ex != null)
                            {
                                downstream.OnError(ex);
                                DisposeAll();
                                continue;
                            }
                        }

                        var d = Volatile.Read(ref done);
                        var a = Volatile.Read(ref active);

                        var q = GetQueue();

                        var v = default(T);

                        var success = q != null && q.TryDequeue(out v);

                        if (d && a == 0 && !success)
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
                        }
                        else
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
        }
    }

    internal abstract class ObservableSourceMergeBase<T> : IDisposable
    {
        protected readonly ISignalObserver<T> downstream;

        protected readonly bool delayErrors;

        HashSet<IDisposable> observers;

        ConcurrentQueue<T> queue;

        protected Exception errors;

        protected int wip;

        protected bool disposed;

        protected int active;

        protected bool done;

        internal ObservableSourceMergeBase(ISignalObserver<T> downstream, bool delayErrors)
        {
            this.downstream = downstream;
            this.delayErrors = delayErrors;
            this.observers = new HashSet<IDisposable>();
        }

        public void Dispose()
        {
            DisposeAll();
            Drain();
        }

        internal void SetDone()
        {
            Volatile.Write(ref done, true);
            Drain();
        }

        internal bool SubscribeTo(IObservableSource<T> source)
        {
            var inner = new InnerObserver(this);
            if (Add(inner))
            {
                Interlocked.Increment(ref active);
                source.Subscribe(inner);
                return true;
            }
            return false;
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

        protected void DisposeAll()
        {
            if (Volatile.Read(ref disposed))
            {
                return;
            }
            var obs = default(HashSet<IDisposable>);
            lock (this)
            {
                if (Volatile.Read(ref disposed))
                {
                    return;
                }

                obs = observers;
                observers = null;
                Volatile.Write(ref disposed, true);
            }

            foreach (var o in obs)
            {
                o.Dispose();
            }
        }

        void InnerNext(InnerObserver sender, T item)
        {
            if (Interlocked.CompareExchange(ref wip, 1, 0) == 0)
            {
                downstream.OnNext(item);
                if (Interlocked.Decrement(ref wip) == 0)
                {
                    return;
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

        ConcurrentQueue<T> GetOrCreateQueue()
        {
            var q = Volatile.Read(ref queue);
            if (q == null)
            {
                q = new ConcurrentQueue<T>();
                var p = Interlocked.CompareExchange(ref queue, q, null);
                if (p != null)
                {
                    return p;
                }
            }
            return q;
        }

        protected ConcurrentQueue<T> GetQueue()
        {
            return Volatile.Read(ref queue);
        }

        internal void Error(Exception ex)
        {
            if (delayErrors)
            {
                ExceptionHelper.AddException(ref errors, ex);
            }
            else
            {
                Interlocked.CompareExchange(ref errors, ex, null);
            }
            SetDone();
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

        void InnerCompleted(InnerObserver sender)
        {
            Interlocked.Decrement(ref active);
            Drain();
        }

        internal void Drain()
        {
            if (Interlocked.Increment(ref wip) == 1)
            {
                DrainLoop();
            }
        }

        protected abstract void DrainLoop();

        internal sealed class InnerObserver : ISignalObserver<T>, IDisposable
        {
            readonly ObservableSourceMergeBase<T> parent;

            IDisposable upstream;

            public InnerObserver(ObservableSourceMergeBase<T> parent)
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

            public void OnError(Exception ex)
            {
                parent.InnerError(this, ex);
            }

            public void OnNext(T item)
            {
                parent.InnerNext(this, item);
            }

            public void OnSubscribe(IDisposable d)
            {
                DisposableHelper.SetOnce(ref upstream, d);
            }
        }
    }

    /// <summary>
    /// Merges some or all observables provided via an array.
    /// </summary>
    /// <typeparam name="T">The result element type.</typeparam>
    /// <remarks>Since 0.0.23</remarks>
    internal sealed class ObservableSourceMergeEnumerable<T> : IObservableSource<T>
    {
        readonly IEnumerable<IObservableSource<T>> sources;

        readonly bool delayErrors;
        readonly int maxConcurrency;

        internal ObservableSourceMergeEnumerable(IEnumerable<IObservableSource<T>> sources, bool delayErrors, int maxConcurrency)
        {
            this.sources = sources;
            this.delayErrors = delayErrors;
            this.maxConcurrency = maxConcurrency;
        }

        public void Subscribe(ISignalObserver<T> observer)
        {
            if (maxConcurrency == int.MaxValue)
            {
                var en = default(IEnumerator<IObservableSource<T>>);
                try
                {
                    en = RequireNonNullRef(sources.GetEnumerator(), "The GetEnumerator returned a null IEnumerator");
                }
                catch (Exception ex)
                {
                    DisposableHelper.Error(observer, ex);
                    return;
                }

                var parent = new MergeMaxCoordinator(observer, delayErrors);
                observer.OnSubscribe(parent);

                for (; ; )
                {
                    var b = false;
                    var src = default(IObservableSource<T>);

                    try
                    {
                        b = en.MoveNext();
                        if (b)
                        {
                            src = RequireNonNullRef(en.Current, "The enumerator returned a null IObservableSource");
                        }
                    }
                    catch (Exception ex)
                    {
                        parent.Error(ex);
                        en.Dispose();
                        return;
                    }

                    if (b)
                    {
                        if (!parent.SubscribeTo(src))
                        {
                            en.Dispose();
                            break;
                        }
                    }
                    else
                    {
                        en.Dispose();
                        break;
                    }
                }

                parent.SetDone();
            }
            else
            {
                var en = default(IEnumerator<IObservableSource<T>>);
                try
                {
                    en = RequireNonNullRef(sources.GetEnumerator(), "The GetEnumerator returned a null IEnumerator");
                }
                catch (Exception ex)
                {
                    DisposableHelper.Error(observer, ex);
                    return;
                }

                var parent = new MergeLimitedCoordinator(observer, delayErrors, maxConcurrency, en);
                observer.OnSubscribe(parent);

                parent.Drain();
            }
        }

        sealed class MergeLimitedCoordinator : ObservableSourceMergeBase<T>
        {
            readonly IEnumerator<IObservableSource<T>> enumerator;

            readonly int maxConcurrency;

            bool noMoreSources;

            internal MergeLimitedCoordinator(ISignalObserver<T> downstream, bool delayErrors, int maxConcurrency, IEnumerator<IObservableSource<T>> enumerator) : base(downstream, delayErrors)
            {
                this.maxConcurrency = maxConcurrency;
                this.enumerator = enumerator;
            }

            protected override void DrainLoop()
            {
                var missed = 1;
                var delayErrors = this.delayErrors;
                var downstream = this.downstream;
                var enumerator = this.enumerator;

                for (; ; )
                {
                    if (Volatile.Read(ref disposed))
                    {
                        var q = GetQueue();
                        if (q != null)
                        {
                            while (q.TryDequeue(out var _)) ;
                        }
                        enumerator?.Dispose();
                        enumerator = null;
                    }
                    else
                    {
                        if (!delayErrors)
                        {
                            var ex = Volatile.Read(ref errors);
                            if (ex != null)
                            {
                                downstream.OnError(ex);
                                DisposeAll();
                                continue;
                            }
                        }

                        var a = Volatile.Read(ref active);

                        if (!noMoreSources && a < maxConcurrency)
                        {
                            var b = false;
                            var src = default(IObservableSource<T>);
                            try
                            {
                                b = enumerator.MoveNext();
                                if (b)
                                {
                                    src = RequireNonNullRef(enumerator.Current, "The enumerator returned a null IObservableSource");
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
                                noMoreSources = true;
                                enumerator.Dispose();
                                enumerator = null;
                                continue;
                            }
                            if (b)
                            {
                                SubscribeTo(src);
                            }
                            else
                            {
                                noMoreSources = true;
                                enumerator.Dispose();
                                enumerator = null;
                            }
                            continue;
                        }

                        var q = GetQueue();

                        var v = default(T);

                        var success = q != null && q.TryDequeue(out v);

                        if (a == 0 && !success)
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
                        }
                        else
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
        }

        sealed class MergeMaxCoordinator : ObservableSourceMergeBase<T>
        {
            internal MergeMaxCoordinator(ISignalObserver<T> downstream, bool delayErrors) : base(downstream, delayErrors)
            {
            }

            protected override void DrainLoop()
            {
                var missed = 1;
                var delayErrors = this.delayErrors;
                var downstream = this.downstream;

                for (; ; )
                {
                    if (Volatile.Read(ref disposed))
                    {
                        var q = GetQueue();
                        if (q != null)
                        {
                            while (q.TryDequeue(out var _)) ;
                        }
                    }
                    else
                    {
                        if (!delayErrors)
                        {
                            var ex = Volatile.Read(ref errors);
                            if (ex != null)
                            {
                                downstream.OnError(ex);
                                DisposeAll();
                                continue;
                            }
                        }

                        var d = Volatile.Read(ref done);
                        var a = Volatile.Read(ref active);

                        var q = GetQueue();

                        var v = default(T);

                        var success = q != null && q.TryDequeue(out v);

                        if (d && a == 0 && !success)
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
                        }
                        else
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
        }
    }
}
