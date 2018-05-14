using System;
using System.Collections.Generic;
using System.Text;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// A basic disposable that can be checked for a disposed state.
    /// </summary>
    internal sealed class BooleanDisposable : IDisposable
    {
        volatile bool disposed;

        /// <summary>
        /// Returns true if this BooleanDisposable has been disposed.
        /// </summary>
        /// <returns>True if this BooleanDisposable has been disposed.</returns>
        public bool IsDisposed()
        {
            return disposed;
        }

        public void Dispose()
        {
            disposed = true;
        }
    }
}
