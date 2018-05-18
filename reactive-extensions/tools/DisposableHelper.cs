using System;
using System.Threading;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("reactive-extensions-test")]
[assembly: InternalsVisibleTo("reactive-extensions-benchmarks")]

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Utility methods for dealing with IDisposable references.
    /// Common IDisposable instances
    /// </summary>
    internal static class DisposableHelper
    {
        /// <summary>
        /// The shared empty, no-op IDisposable.
        /// </summary>
        internal static readonly IDisposable EMPTY = new EmptyDisposable();

        /// <summary>
        /// The disposable indicating a disposed reference. Do not leak this!
        /// </summary>
        internal static readonly IDisposable DISPOSED = new DisposedDisposable();

        /// <summary>
        /// Represents an empty disposable that does nothing upon dispose.
        /// </summary>
        sealed class EmptyDisposable : IDisposable
        {
            public void Dispose()
            {
                // deliberately no-op
            }
        }

        /// <summary>
        /// Represents a disposable that indicates an <see cref="IDisposable"/> field
        /// has been marked as disposed.
        /// </summary>
        sealed class DisposedDisposable : IDisposable
        {
            public void Dispose()
            {
                // deliberately no-op
            }
        }

        /// <summary>
        /// Atomically set the target field to contain the shared
        /// disposed indicator and dispose any previous <see cref="IDisposable"/>
        /// it contained.
        /// </summary>
        /// <param name="field">The target field to dispose the contents of.</param>
        /// <returns>True if the current thread disposed the contents, false
        /// if it was disposed already or concurrently by some other thread.</returns>
        internal static bool Dispose(ref IDisposable field)
        {
            var d = Volatile.Read(ref field);
            if (d != DISPOSED)
            {
                d = Interlocked.Exchange(ref field, DISPOSED);
                if (d != DISPOSED)
                {
                    d?.Dispose();
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Sets the target <paramref name="field"/> to contain
        /// the common disposed indicator value using
        /// atomic release-write (store-store barrier).
        /// </summary>
        /// <param name="field">The target field to set.</param>
        /// <remarks>Since 0.0.6</remarks>
        internal static void WeakDispose(ref IDisposable field)
        {
            Volatile.Write(ref field, DisposableHelper.DISPOSED);
        }

        /// <summary>
        /// Checks if the given field contains the common
        /// disposed indicator in a thread-safe manner.
        /// </summary>
        /// <param name="field">The target field to check.</param>
        /// <returns></returns>
        internal static bool IsDisposed(ref IDisposable field)
        {
            return Volatile.Read(ref field) == DISPOSED;
        }

        internal static bool Set(ref IDisposable field, IDisposable next)
        {
            for (; ; )
            {
                var c = Volatile.Read(ref field);
                if (c == DISPOSED)
                {
                    next?.Dispose();
                    return false;
                }
                if (Interlocked.CompareExchange(ref field, next, c) == c)
                {
                    c?.Dispose();
                    return true;
                }
            }
        }

        internal static bool Replace(ref IDisposable field, IDisposable next)
        {
            for (; ; )
            {
                var c = Volatile.Read(ref field);
                if (c == DISPOSED)
                {
                    next?.Dispose();
                    return false;
                }
                if (Interlocked.CompareExchange(ref field, next, c) == c)
                {
                    return true;
                }
            }
        }

        internal static bool SetOnce(ref IDisposable field, IDisposable only)
        {
            if (only == null)
            {
                throw new ArgumentNullException(nameof(only));
            }
            if (Interlocked.CompareExchange(ref field, only, null) != null)
            {
                only.Dispose();
                return false;
            }
            return true;
        }

        internal static bool SetIfNull(ref IDisposable field, IDisposable only)
        {
            if (only == null)
            {
                throw new ArgumentNullException(nameof(only));
            }
            if (Volatile.Read(ref field) == DISPOSED)
            {
                only.Dispose();
                return false;
            }
            if (Interlocked.CompareExchange(ref field, only, null) != null)
            {
                only.Dispose();
                return false;
            }
            return true;
        }

        /// <summary>
        /// Replace the disposable in the field if it is null,
        /// dispose <paramref name="only"/> if the field is disposed.
        /// </summary>
        /// <param name="field">The target field.</param>
        /// <param name="only">The new disposable to replace the contents of the field with.</param>
        /// <returns>True if successful, false if the field was not null</returns>
        internal static bool ReplaceIfNull(ref IDisposable field, IDisposable only)
        {
            if (only == null)
            {
                throw new ArgumentNullException(nameof(only));
            }
            if (Volatile.Read(ref field) == DISPOSED)
            {
                only.Dispose();
                return false;
            }
            return Interlocked.CompareExchange(ref field, only, null) == null;
        }

        internal static void Complete(this ICompletableObserver observer)
        {
            observer.OnSubscribe(EMPTY);
            observer.OnCompleted();
        }

        internal static void Complete<T>(this IMaybeObserver<T> observer)
        {
            observer.OnSubscribe(EMPTY);
            observer.OnCompleted();
        }

        internal static void Error<T>(this IMaybeObserver<T> observer, Exception error)
        {
            observer.OnSubscribe(EMPTY);
            observer.OnError(error);
        }

        internal static void Error<T>(this ISingleObserver<T> observer, Exception error)
        {
            observer.OnSubscribe(EMPTY);
            observer.OnError(error);
        }

        internal static void Error(this ICompletableObserver observer, Exception error)
        {
            observer.OnSubscribe(EMPTY);
            observer.OnError(error);
        }
    }
}
