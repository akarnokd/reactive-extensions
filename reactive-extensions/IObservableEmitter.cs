using System;
using System.Collections.Generic;
using System.Text;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Abstraction over an <see cref="IObserver{T}"/> with
    /// the ability to check for a disposed state and
    /// register a disposable resource with it.
    /// </summary>
    /// <typeparam name="T">The element type of the sequence.</typeparam>
    /// <remarks>Since 0.0.5</remarks>
    public interface IObservableEmitter<T> : IObserver<T>
    {
        /// <summary>
        /// Returns true if the downstream has disposed the sequence.
        /// </summary>
        /// <returns>True if the downstream has disposed the sequence.</returns>
        bool IsDisposed();

        /// <summary>
        /// Associates a disposable resource with this emitter,
        /// disposing any previously set resource. The resource is
        /// then automatically disposed after the emitter has
        /// terminated or the downstream disposes the sequence.
        /// </summary>
        /// <param name="resource">The resource to associate</param>
        void SetResource(IDisposable resource);
    }
}
