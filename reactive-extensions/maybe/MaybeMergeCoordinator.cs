using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    internal abstract class MaybeMergeCoordinator<T> : IDisposable
    {
        protected readonly IObserver<T> downstream;

        protected readonly bool delayErrors;

        HashSet<IDisposable> observers;

        ConcurrentQueue<T> queue;

        protected int wip;

        protected bool disposed;

        protected Exception errors;

        protected MaybeMergeCoordinator(IObserver<T> downstream, bool delayErrors)
        {
            this.downstream = downstream;
            this.delayErrors = delayErrors;
            this.observers = new HashSet<IDisposable>();
        }

        internal bool IsDisposed()
        {
            return Volatile.Read(ref disposed);
        }

        public void Dispose()
        {
            if (IsDisposed())
            {
                return;
            }

            var o = default(HashSet<IDisposable>);
            lock (this)
            {
                if (IsDisposed())
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

        internal bool SubscribeTo(IMaybeSource<T> source)
        {
            var inner = new InnerObserver(this);
            if (Add(inner))
            {
                source.Subscribe(inner);
                return true;
            }
            return false;
        }

        internal bool Add(InnerObserver inner)
        {
            if (IsDisposed())
            {
                return false;
            }

            lock (this)
            {
                if (IsDisposed())
                {
                    return false;
                }
                observers.Add(inner);
            }
            return true;
        }

        internal void Remove(InnerObserver inner)
        {
            if (IsDisposed())
            {
                return;
            }

            lock (this)
            {
                if (IsDisposed())
                {
                    return;
                }
                observers.Remove(inner);
            }
        }

        internal ConcurrentQueue<T> GetQueue()
        {
            return Volatile.Read(ref queue);
        }

        internal ConcurrentQueue<T> CreateQueue()
        {
            var q = new ConcurrentQueue<T>();
            var p = Interlocked.CompareExchange(ref queue, q, null);
            return p ?? q;
        }

        internal ConcurrentQueue<T> GetOrCreateQueue()
        {
            return GetQueue() ?? CreateQueue();
        }

        internal abstract void InnerSuccess(InnerObserver sender, T item);

        internal abstract void InnerError(InnerObserver sender, Exception ex);

        internal abstract void InnerCompleted(InnerObserver sender);

        internal void Drain()
        {
            if (Interlocked.Increment(ref wip) == 1)
            {
                DrainLoop();
            }
        }

        internal abstract void DrainLoop();

        internal sealed class InnerObserver : IMaybeObserver<T>, IDisposable
        {
            readonly MaybeMergeCoordinator<T> parent;

            IDisposable upstream;

            public InnerObserver(MaybeMergeCoordinator<T> parent)
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

            public void OnSuccess(T item)
            {
                parent.InnerSuccess(this, item);
            }
        }
    }
}
