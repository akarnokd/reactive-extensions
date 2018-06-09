using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// A hot observable source that dispatches signals to
    /// one or more observers.
    /// </summary>
    /// <typeparam name="T">The element type of the sequence.</typeparam>
    /// <remarks>Since 0.0.17</remarks>
    public sealed class PublishSubject<T> : IObservableSubject<T>, IDisposable, ISubjectExtensions
    {
        readonly bool refCount;

        PublishDisposable[] observers;

        IDisposable upstream;

        Exception error;

        static readonly PublishDisposable[] Empty = new PublishDisposable[0];
        static readonly PublishDisposable[] Terminated = new PublishDisposable[0];

        static readonly IDisposable Prepared = new BooleanDisposable();

        /// <summary>
        /// Returns true if this subject has any observers.
        /// </summary>
        public bool HasObservers => HasObserver();

        /// <summary>
        /// Constructs a fresh PublishSubject.
        /// </summary>
        /// <param name="refCount">If true, the last observer will also dispose
        /// the upstream connection and further observers receive a terminal event.</param>
        public PublishSubject(bool refCount = false)
        {
            this.refCount = refCount;
            Volatile.Write(ref observers, Empty);
        }

        /// <summary>
        /// Disposes the upstream connection.
        /// </summary>
        public void Dispose()
        {
            DisposableHelper.Dispose(ref upstream);
        }

        /// <summary>
        /// Returns the terminal exception if present.
        /// </summary>
        /// <returns>The terminal exception or null if not terminated
        /// or terminated normally.</returns>
        public Exception GetException()
        {
            var ex = Volatile.Read(ref error);
            return ex != ExceptionHelper.TERMINATED ? ex : null;
        }

        /// <summary>
        /// Returns true if this subject has been terminated
        /// via <see cref="OnCompleted"/>.
        /// </summary>
        /// <returns>True if this subject terminated normally.</returns>
        public bool HasCompleted()
        {
            return Volatile.Read(ref observers) == Terminated && error == ExceptionHelper.TERMINATED;
        }

        /// <summary>
        /// Returns true if this subject has been
        /// terminated via <see cref="OnError(Exception)"/>.
        /// </summary>
        /// <returns>True if this subject has been terminated with an error.</returns>
        public bool HasException()
        {
            return Volatile.Read(ref observers) == Terminated && error != ExceptionHelper.TERMINATED;
        }

        /// <summary>
        /// Returns true if this subject has observers.
        /// </summary>
        /// <returns>True if this subject has observers</returns>
        public bool HasObserver()
        {
            return Volatile.Read(ref observers).Length != 0;
        }

        /// <summary>
        /// Dispatch an error to all current or late observers.
        /// </summary>
        public void OnCompleted()
        {
            if (Interlocked.CompareExchange(ref error, ExceptionHelper.TERMINATED, null) == null)
            {
                foreach (var inner in Interlocked.Exchange(ref observers, Terminated))
                {
                    inner.OnCompleted();
                }
            }
        }

        /// <summary>
        /// Dispatch an error to all current or
        /// late observers.
        /// </summary>
        /// <param name="ex">The error to dispatch or retain.</param>
        public void OnError(Exception ex)
        {
            ValidationHelper.RequireNonNullRef(ex, nameof(ex));

            if (Interlocked.CompareExchange(ref error, ex, null) == null)
            {
                foreach (var inner in Interlocked.Exchange(ref observers, Terminated))
                {
                    inner.OnError(ex);
                }
            }
        }

        /// <summary>
        /// Dispatch an item to all current observers.
        /// </summary>
        /// <param name="item">The item to dispatch.</param>
        public void OnNext(T item)
        {
            foreach (var inner in Volatile.Read(ref observers))
            {
                inner.OnNext(item);
            }
        }

        /// <summary>
        /// Optional call: set the upstream disposable
        /// to later dispose via <see cref="Dispose"/>.
        /// </summary>
        /// <param name="d">The upstream disposable.</param>
        public void OnSubscribe(IDisposable d)
        {
            for (; ; )
            {
                var curr = Volatile.Read(ref upstream);
                if (curr != null && curr != Prepared)
                {
                    d?.Dispose();
                    return;
                }
                if (Interlocked.CompareExchange(ref upstream, d, curr) == curr)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Prepare this subject by trying to set the
        /// prepared indicator (ignored by OnSubscribe)
        /// and return true if succeeded.
        /// </summary>
        /// <returns>True if the prepared instance could be swapped in</returns>
        /// <remarks>Since 0.0.22</remarks>
        internal bool Prepare()
        {
            return Interlocked.CompareExchange(ref upstream, Prepared, null) == null;
        }

        /// <summary>
        /// Subscribes the observer to this subject and
        /// relays the live items or replays the terminal
        /// event to it.
        /// </summary>
        /// <param name="observer">The observer to dispatch signals to.</param>
        public void Subscribe(ISignalObserver<T> observer)
        {
            ValidationHelper.RequireNonNullRef(observer, nameof(observer));

            var parent = new PublishDisposable(observer, this);
            observer.OnSubscribe(parent);
            if (Add(parent))
            {
                if (parent.IsDisposed())
                {
                    Remove(parent);
                }
            }
            else
            {
                var ex = error;

                if (ex != ExceptionHelper.TERMINATED)
                {
                    parent.OnError(ex);
                }
                else
                {
                    parent.OnCompleted();
                }
            }
        }

        bool Add(PublishDisposable inner)
        {
            for (; ;)
            {
                var a = Volatile.Read(ref observers);
                if (a == Terminated)
                {
                    return false;
                }
                var n = a.Length;
                var b = new PublishDisposable[n + 1];
                Array.Copy(a, 0, b, 0, n);
                b[n] = inner;
                if (Interlocked.CompareExchange(ref observers, b, a) == a)
                {
                    return true;
                }
            }
        }

        void Remove(PublishDisposable inner)
        {
            for (; ;)
            {
                var a = Volatile.Read(ref observers);
                var n = a.Length;
                if (n == 0)
                {
                    break;
                }
                int j = Array.IndexOf(a, inner);
                if (j < 0)
                {
                    break;
                }
                var b = default(PublishDisposable[]);

                if (n == 1)
                {
                    if (refCount)
                    {
                        if (Interlocked.CompareExchange(ref observers, Terminated, a) == a)
                        {
                            DisposableHelper.Dispose(ref upstream);
                            break;
                        }
                        continue;
                    }

                    b = Empty;
                }
                else
                {
                    b = new PublishDisposable[n - 1];
                    Array.Copy(a, 0, b, 0, j);
                    Array.Copy(a, j + 1, b, j, n - j - 1);
                }
                if (Interlocked.CompareExchange(ref observers, b, a) == a)
                {
                    break;
                }
            }
        }

        sealed class PublishDisposable : IDisposable
        {
            ISignalObserver<T> downstream;

            PublishSubject<T> parent;

            public PublishDisposable(ISignalObserver<T> downstream, PublishSubject<T> parent)
            {
                this.downstream = downstream;
                Volatile.Write(ref this.parent, parent);
            }

            public void Dispose()
            {
                if (Interlocked.Exchange(ref downstream, null) != null)
                {
                    parent.Remove(this);
                    parent = null;
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
        }
    }
}
