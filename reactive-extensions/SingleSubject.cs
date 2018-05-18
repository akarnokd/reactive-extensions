using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using static akarnokd.reactive_extensions.ValidationHelper;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// A single-based, hot subject that multicasts the success or termination event
    /// it receives through its single observer API surface.
    /// </summary>
    /// <remarks>Since 0.0.9</remarks>
    public sealed class SingleSubject<T> : ISingleSource<T>, ISingleObserver<T>, ISubjectExtensions
    {
        static readonly InnerDisposable[] EMPTY = new InnerDisposable[0];

        static readonly InnerDisposable[] TERMINATED = new InnerDisposable[0];

        readonly bool refCount;

        InnerDisposable[] observers;

        IDisposable upstream;

        int once;

        Exception error;

        T value;

        /// <summary>
        /// Construct an active (non-terminated) single subject.
        /// </summary>
        /// <param name="refCount">If true, the subject acts as a reference-counter
        /// and disposes its upstream when all observers have themselves disposed from
        /// this single subject.</param>
        public SingleSubject(bool refCount = false)
        {
            this.refCount = refCount;
            Volatile.Write(ref observers, EMPTY);
        }

        /// <summary>
        /// Returns the terminal exception, if any.
        /// </summary>
        /// <returns>The terminal exception or null if the subject has not yet terminated or not with an error.</returns>
        public Exception GetException()
        {
            var ex = Volatile.Read(ref observers);
            return ex == TERMINATED ? error : null;
        }

        /// <summary>
        /// Returns true if this subject terminated successfully.
        /// </summary>
        /// <returns>True if this subject terminated successfully.</returns>
        public bool HasCompleted()
        {
            return Volatile.Read(ref observers) == TERMINATED && error == null;
        }

        /// <summary>
        /// Returns true if this subject terminated with an error.
        /// </summary>
        /// <returns>True if this subject terminated with an error.</returns>
        public bool HasException()
        {
            return Volatile.Read(ref observers) == TERMINATED && error != null;
        }

        /// <summary>
        /// Returns true if there is at least one observer currently subscribed to
        /// this ISubject.
        /// </summary>
        /// <returns>True if there is an observer currently subscribed.</returns>
        public bool HasObserver()
        {
            return Volatile.Read(ref observers).Length != 0;
        }

        /// <summary>
        /// Terminates the subject with an error and notifies
        /// any current or future observers with this exception.
        /// </summary>
        /// <param name="error">The exception to terminate this subject with.</param>
        public void OnError(Exception error)
        {
            RequireNonNullRef(error, "error is null");

            if (Interlocked.CompareExchange(ref once, 1, 0) == 0)
            {
                this.error = error;
                foreach (var inner in Interlocked.Exchange(ref observers, TERMINATED))
                {
                    if (!inner.IsDisposed())
                    {
                        inner.downstream.OnError(error);
                    }
                }
            }

            DisposableHelper.Dispose(ref upstream);
        }

        /// <summary>
        /// Terminates the subject with an error and notifies
        /// any current or future observers with this exception.
        /// </summary>
        /// <param name="item">The success item.</param>
        public void OnSuccess(T item)
        {
            if (Interlocked.CompareExchange(ref once, 1, 0) == 0)
            {
                this.value = item;
                foreach (var inner in Interlocked.Exchange(ref observers, TERMINATED))
                {
                    if (!inner.IsDisposed())
                    {
                        inner.downstream.OnSuccess(item);
                    }
                }
            }

            DisposableHelper.Dispose(ref upstream);
        }

        /// <summary>
        /// Sets an upstream disposable on this subject.
        /// </summary>
        /// <param name="d">The upstream disposable connection.</param>
        public void OnSubscribe(IDisposable d)
        {
            DisposableHelper.SetOnce(ref upstream, d);
        }

        /// <summary>
        /// Subscribes the given completable observer to this subject
        /// and relays/replays the terminal events of the subject.
        /// </summary>
        /// <param name="observer">The completable observer that wants to listen to the terminal events.</param>
        public void Subscribe(ISingleObserver<T> observer)
        {
            var inner = new InnerDisposable(observer, this);
            observer.OnSubscribe(inner);

            if (Add(inner))
            {
                if (inner.IsDisposed())
                {
                    Remove(inner);
                }
            }
            else
            {
                if (!inner.IsDisposed())
                {
                    var ex = Volatile.Read(ref error);
                    if (ex == null)
                    {
                        observer.OnSuccess(value);
                    }
                    else
                    {
                        observer.OnError(ex);
                    }
                }
            }
        }

        bool Add(InnerDisposable inner)
        {
            for (; ; )
            {
                var a = Volatile.Read(ref observers);
                if (a == TERMINATED)
                {
                    return false;
                }

                var n = a.Length;
                var b = new InnerDisposable[n + 1];
                Array.Copy(a, 0, b, 0, n);
                b[n] = inner;
                if (Interlocked.CompareExchange(ref observers, b, a) == a)
                {
                    return true;
                }
            }
        }

        void Remove(InnerDisposable inner)
        {
            for (; ; )
            {
                var a = Volatile.Read(ref observers);
                var n = a.Length;
                if (n == 0)
                {
                    break;
                }

                var j = -1;
                for (int i = 0; i < n; i++)
                {
                    if (a[i] == inner)
                    {
                        j = i;
                        break;
                    }
                }

                if (j < 0)
                {
                    break;
                }

                var b = default(InnerDisposable[]);
                if (n == 1)
                {
                    if (refCount)
                    {
                        if (Interlocked.CompareExchange(ref once, 1, 0) == 0)
                        {
                            this.error = new OperationCanceledException();
                            if (Interlocked.CompareExchange(ref observers, TERMINATED, a) == a)
                            {
                                DisposableHelper.Dispose(ref upstream);
                                break;
                            }
                        }
                        continue;
                    }
                    else
                    {
                        b = EMPTY;
                    }
                }
                else
                {
                    b = new InnerDisposable[n - 1];
                    Array.Copy(a, 0, b, 0, j);
                    Array.Copy(a, j + 1, b, j, n - j - 1);
                }
                if (Interlocked.CompareExchange(ref observers, b, a) == a)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Returns true if this single subject was terminated with
        /// a success value.
        /// </summary>
        /// <returns>True if this single subject was terminated with a
        /// success value.</returns>
        public bool HasValue()
        {
            return Volatile.Read(ref observers) == TERMINATED && error == null;
        }

        /// <summary>
        /// Tries to retrieve the success value of this single subject
        /// if it has been terminated with one.
        /// </summary>
        /// <param name="value">The value read out.</param>
        /// <returns>True if this single subject terminated with a value, false if
        /// it is still running.</returns>
        public bool TryGetValue(out T value)
        {
            if (Volatile.Read(ref observers) == TERMINATED && error == null)
            {
                value = this.value;
                return true;
            }
            value = default(T);
            return false;
        }

        internal sealed class InnerDisposable : IDisposable
        {
            internal readonly ISingleObserver<T> downstream;

            SingleSubject<T> parent;

            public InnerDisposable(ISingleObserver<T> downstream, SingleSubject<T> parent)
            {
                this.downstream = downstream;
                Volatile.Write(ref this.parent, parent);
            }

            public void Dispose()
            {
                Interlocked.Exchange(ref parent, null)?.Remove(this);
            }

            internal bool IsDisposed()
            {
                return Volatile.Read(ref parent) == null;
            }
        }
    }
}
