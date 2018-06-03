using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// A hot observable sequence that allows only one observer
    /// during its lifetime and cached items until that single
    /// observer subscribes.
    /// </summary>
    /// <typeparam name="T">The element type of the sequence.</typeparam>
    public sealed class MonocastSubject<T> : IObservableSource<T>, ISignalObserver<T>, ISubjectExtensions, IDisposable
    {
        readonly SpscLinkedArrayQueue<T> queue;

        MonocastDisposable observer;

        IDisposable upstream;

        int once;

        int wip;

        Exception error;

        Action onTerminate;

        bool outputFused;

        /// <summary>
        /// Returns true if this subject has an observer.
        /// </summary>
        public bool HasObservers => HasObserver();

        /// <summary>
        /// Constructs a MonocastSubject with the default
        /// or specified capacity hint for the internal queue.
        /// </summary>
        /// <param name="capacityHint">The expected number of items to be
        /// cached.</param>
        /// <param name="onTerminate">Called when the upstream terminates
        /// or the single observer disposes, at most once.</param>
        public MonocastSubject(int capacityHint = 32, Action onTerminate = null)
        {
            this.onTerminate = onTerminate;
            queue = new SpscLinkedArrayQueue<T>(capacityHint);
        }

        /// <summary>
        /// Returns the terminal exception of this subject if any.
        /// </summary>
        /// <returns>The terminal exception or null if not yet terminated
        /// or completed normally.</returns>
        public Exception GetException()
        {
            var ex = Volatile.Read(ref error);
            return ex != ExceptionHelper.TERMINATED ? ex : null;
        }

        /// <summary>
        /// Returns true if this subject was completed normally
        /// via <see cref="OnCompleted"/>.
        /// </summary>
        /// <returns>True if the subject has been terminated with an error.</returns>
        public bool HasCompleted()
        {
            return Volatile.Read(ref error) == ExceptionHelper.TERMINATED;
        }

        /// <summary>
        /// Returns true if this subject was terminated by an error
        /// via <see cref="OnError(Exception)"/>.
        /// </summary>
        /// <returns>True if the subject has been terminated with an error.</returns>
        public bool HasException()
        {
            var ex = Volatile.Read(ref error);
            return ex != null && ex != ExceptionHelper.TERMINATED;
        }

        /// <summary>
        /// Returns true if this subject has an observer.
        /// </summary>
        /// <returns>True if this subject has an observer.</returns>
        public bool HasObserver()
        {
            return Volatile.Read(ref observer) != null;
        }

        /// <summary>
        /// Signal or queue up a terminal signal for the single observer.
        /// </summary>
        public void OnCompleted()
        {
            if (Interlocked.CompareExchange(ref error, ExceptionHelper.TERMINATED, null) == null)
            {
                Terminate();
                Drain();
            }
        }

        /// <summary>
        /// Signal or queue up an error for the single observer.
        /// </summary>
        /// <param name="ex">The exception to signal eventually.</param>
        public void OnError(Exception ex)
        {
            ValidationHelper.RequireNonNullRef(ex, nameof(ex));
            if (Interlocked.CompareExchange(ref error, ex, null) == null)
            {
                Terminate();
                Drain();
            }
        }

        /// <summary>
        /// Signal or queue up the item for the single observer.
        /// </summary>
        /// <param name="item">The item to signal eventually.</param>
        public void OnNext(T item)
        {
            queue.Offer(item);
            Drain();
        }

        /// <summary>
        /// Set an upstream disposable connection to be
        /// disposed manually or by the single observer disposing.
        /// </summary>
        /// <param name="d">The disposable to use.-</param>
        public void OnSubscribe(IDisposable d)
        {
            DisposableHelper.SetOnce(ref upstream, d);
        }

        /// <summary>
        /// Subscribes the first observer to this subject and
        /// starts replaying/relaying signals. All future
        /// observers receive an InvalidOperationException.
        /// </summary>
        /// <param name="observer">The observer to subscribe with.</param>
        public void Subscribe(ISignalObserver<T> observer)
        {
            ValidationHelper.RequireNonNullRef(observer, nameof(observer));

            if (Interlocked.CompareExchange(ref once, 1, 0) == 0)
            {
                var inner = new MonocastDisposable(observer, this);
                observer.OnSubscribe(inner);
                if (Interlocked.CompareExchange(ref this.observer, inner, null) == null)
                {
                    if (inner.IsDisposed())
                    {
                        this.observer = null;
                    }
                    else
                    {
                        Drain();
                    }
                }
            }
            else
            {
                DisposableHelper.Error(observer, new InvalidOperationException("MonocastSubject allows at most one observer"));
            }
        }

        void Drain()
        {
            if (Interlocked.Increment(ref wip) != 1)
            {
                return;
            }

            var missed = 1;

            for (; ; )
            {
                var observer = Volatile.Read(ref this.observer);

                if (observer != null)
                {
                    if (observer.IsDisposed())
                    {
                        queue.Clear();
                    }
                    else
                    if (outputFused)
                    {
                        DrainFused(observer);
                    }
                    else
                    {
                        DrainNormal(observer);
                    }
                }

                missed = Interlocked.Add(ref wip, -missed);
                if (missed == 0)
                {
                    break;
                }
            }
        }

        void DrainNormal(MonocastDisposable observer)
        {
            for (; ; )
            {
                if (observer.IsDisposed())
                {
                    queue.Clear();
                    break;
                }
                else
                {
                    var ex = Volatile.Read(ref error);
                    var v = queue.TryPoll(out var success);
                    var empty = !success;

                    if (ex != null && empty)
                    {
                        if (ex == ExceptionHelper.TERMINATED)
                        {
                            observer.OnCompleted();
                        }
                        else
                        {
                            observer.OnError(ex);
                        }
                        this.observer = null;
                        break;
                    }

                    if (empty)
                    {
                        break;
                    }

                    observer.OnNext(v);
                }
            }
        }

        void DrainFused(MonocastDisposable observer)
        {
            var ex = Volatile.Read(ref error);

            if (!queue.IsEmpty())
            {
                observer.OnNext(default(T));
            }

            if (ex != null)
            {
                if (ex == ExceptionHelper.TERMINATED)
                {
                    observer.OnCompleted();
                }
                else
                {
                    observer.OnError(ex);
                }
                this.observer = null;
            }
        }

        void Terminate()
        {
            if (Volatile.Read(ref onTerminate) != null)
            {
                Interlocked.Exchange(ref onTerminate, null)?.Invoke();
            }
        }

        void Remove(MonocastDisposable inner)
        {
            Interlocked.CompareExchange(ref observer, null, inner);
            Terminate();
            Dispose();
        }

        /// <summary>
        /// Disposes the upstream connection if any.
        /// </summary>
        public void Dispose()
        {
            DisposableHelper.Dispose(ref upstream);
        }

        int RequestFusion(int mode)
        {
            if ((mode & FusionSupport.Async) != 0)
            {
                outputFused = true;
                return FusionSupport.Async;
            }
            return FusionSupport.None;
        }

        T TryPoll(out bool success)
        {
            return queue.TryPoll(out success);
        }

        bool IsEmpty()
        {
            return queue.IsEmpty();
        }

        void Clear()
        {
            queue.Clear();
        }

        sealed class MonocastDisposable : IFuseableDisposable<T>
        {
            readonly MonocastSubject<T> parent;

            ISignalObserver<T> downstream;

            public MonocastDisposable(ISignalObserver<T> downstream, MonocastSubject<T> parent)
            {
                this.parent = parent;
                Volatile.Write(ref this.downstream, downstream);
            }

            public void Dispose()
            {
                if (Interlocked.Exchange(ref downstream, null) != null)
                {
                    parent.Remove(this);
                }
            }

            internal void OnNext(T item)
            {
                Volatile.Read(ref downstream)?.OnNext(item);
            }

            internal void OnError(Exception ex)
            {
                Volatile.Read(ref downstream)?.OnError(ex);
            }

            internal void OnCompleted()
            {
                Volatile.Read(ref downstream)?.OnCompleted();
            }

            internal bool IsDisposed()
            {
                return Volatile.Read(ref downstream) == null;
            }

            public int RequestFusion(int mode)
            {
                return parent.RequestFusion(mode);
            }

            public T TryPoll(out bool success)
            {
                return parent.TryPoll(out success);
            }

            public bool IsEmpty()
            {
                return parent.IsEmpty();
            }

            public void Clear()
            {
                parent.Clear();
            }

            public bool TryOffer(T item)
            {
                throw new InvalidOperationException("Should not be called!");
            }
        }
    }
}
