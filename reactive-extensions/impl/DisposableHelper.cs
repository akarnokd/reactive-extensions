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

        sealed class EmptyDisposable : IDisposable
        {
            public void Dispose()
            {
                // deliberately no-op
            }
        }

        sealed class DisposedDisposable : IDisposable
        {
            public void Dispose()
            {
                // deliberately no-op
            }
        }

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
    }
}
