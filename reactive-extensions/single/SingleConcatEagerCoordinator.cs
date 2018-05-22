using System;
using System.Collections.Concurrent;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Coordinates the observers of a concatenate-eagerly operation.
    /// </summary>
    /// <typeparam name="T">The success value type of the sources.</typeparam>
    /// <remarks>Since 0.0.12</remarks>
    internal abstract class SingleConcatEagerCoordinator<T> : IDisposable
    {
        protected readonly IObserver<T> downstream;

        protected readonly bool delayErrors;

        protected readonly ConcurrentQueue<InnerObserver> observers;

        protected int wip;

        protected bool disposed;

        protected Exception errors;

        protected SingleConcatEagerCoordinator(IObserver<T> downstream, bool delayErrors)
        {
            this.downstream = downstream;
            this.delayErrors = delayErrors;
            this.observers = new ConcurrentQueue<InnerObserver>();
        }

        public virtual void Dispose()
        {
            Volatile.Write(ref disposed, true);
            Cleanup();
        }

        void Cleanup()
        {
            while (observers.TryDequeue(out var inner))
            {
                inner.Dispose();
            }
        }

        void InnerError(InnerObserver sender, Exception error)
        {
            if (delayErrors)
            {
                ExceptionHelper.AddException(ref errors, error);
                sender.SetDone();
            }
            else
            {
                Interlocked.CompareExchange(ref errors, error, null);
            }
            Drain();
        }

        public bool SubscribeTo(ISingleSource<T> source)
        {
            var inner = new InnerObserver(this);
            observers.Enqueue(inner);
            if (Volatile.Read(ref disposed))
            {
                DisposableHelper.WeakDispose(ref inner.upstream);
                Cleanup();
                return false;
            }
            if (source == null)
            {
                InnerError(inner, new NullReferenceException("The ISingleSource is null"));
                return true;
            }
            source.Subscribe(inner);
            return true;
        }

        internal abstract void Drain();

        internal sealed class InnerObserver : ISingleObserver<T>, IDisposable
        {
            readonly SingleConcatEagerCoordinator<T> parent;

            internal IDisposable upstream;

            internal T value;
            int state;

            public InnerObserver(SingleConcatEagerCoordinator<T> parent)
            {
                this.parent = parent;
            }

            public void Dispose()
            {
                DisposableHelper.Dispose(ref upstream);
            }

            internal int GetState()
            {
                return Volatile.Read(ref state);
            }

            internal void SetDone()
            {
                Volatile.Write(ref this.state, 2);
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
                value = item;
                Volatile.Write(ref state, 1);
                parent.Drain();
            }
        }
    }

    /// <summary>
    /// Coordinator for eagerly concatenating all sources.
    /// Use <code>SubscribeTo(ISingleSource{T})</code>
    /// to submit items, then call <see cref="Done"/>.
    /// </summary>
    /// <remarks>Since 0.0.12</remarks>
    internal sealed class SingleConcatEagerAllCoordinator<T> : SingleConcatEagerCoordinator<T>
    {
        bool done;

        internal SingleConcatEagerAllCoordinator(IObserver<T> downstream, bool delayErrors) : base(downstream, delayErrors)
        {
        }

        internal void Done()
        {
            Volatile.Write(ref done, true);
            Drain();
        }

        internal override void Drain()
        {
            if (Interlocked.Increment(ref wip) != 1)
            {
                return;
            }

            var missed = 1;
            var observers = this.observers;
            var downstream = this.downstream;
            var delayErrors = this.delayErrors;

            for (; ; )
            {
                if (Volatile.Read(ref disposed))
                {
                    while (observers.TryDequeue(out var inner))
                    {
                        inner.Dispose();
                    }
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
                            continue;
                        }
                    }

                    var d = Volatile.Read(ref done);

                    var empty = !observers.TryPeek(out var inner);

                    if (d && empty)
                    {
                        Volatile.Write(ref disposed, true);
                        var ex = Volatile.Read(ref errors);
                        if (ex != null)
                        {
                            downstream.OnError(ex);
                        }
                        else
                        {
                            downstream.OnCompleted();
                        }
                    }
                    else
                    if (!empty)
                    {
                        var state = inner.GetState();

                        if (state == 1)
                        {
                            downstream.OnNext(inner.value);
                            observers.TryDequeue(out var _);
                            continue;
                        }
                        else
                        if (state == 2)
                        {
                            observers.TryDequeue(out var _);
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
