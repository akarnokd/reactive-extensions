using System;
using System.Collections.Generic;
using System.Text;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// A reactive base type that only succeeds or signals error.
    /// It follows the <code>OnSubscribe (OnSuccess|OnError)?</code> protocol.
    /// </summary>
    /// <typeparam name="T">The element type of this reactive source.</typeparam>
    /// <remarks>Since 0.0.5</remarks>
    public interface ISingleSource<out T>
    {
        /// <summary>
        /// Subscribes a observer to this single source.
        /// The implementation should call <see cref="ISingleObserver{T}.OnSubscribe(IDisposable)"/>
        /// first with a disposable representing the connection/channel between
        /// this single source and the single observer.
        /// It is the responsibility of the single source to dispose
        /// any resource it uses upon disposing this connection disposable or
        /// when the observer is sent an OnSuccess or OnError signal.
        /// </summary>
        /// <param name="observer">The observer that wants to consume this single.</param>
        void Subscribe(ISingleObserver<T> observer);
    }

    /// <summary>
    /// A consumer of <see cref="ISingleSource{T}"/>s which only
    /// succeeds with one item or signals an error.
    /// It follows the <code>OnSubscribe (OnSuccess|OnError)?</code> protocol.
    /// </summary>
    /// <remarks>Since 0.0.5</remarks>
    public interface ISingleObserver<in T>
    {
        /// <summary>
        /// Sets up the dispose channel between this single observer and
        /// the single source. Should be called before any other
        /// OnXXX method.
        /// </summary>
        /// <param name="d">The disposable representing the channel/connection
        /// between the upstream single source and this single observer.</param>
        void OnSubscribe(IDisposable d);

        /// <summary>
        /// Signal the only success item.
        /// When this signal arrives, the upstream single source
        /// should be considered disposed; there is no need to call
        /// dispose on the connection object.
        /// </summary>
        /// <param name="item">The success item to signal.</param>
        void OnSuccess(T item);

        /// <summary>
        /// Signal a failure with an Exception.
        /// When this signal arrives, the upstream single source
        /// should be considered disposed; there is no need to call
        /// dispose on the connection object.
        /// </summary>
        /// <param name="error">The error to signal.</param>
        void OnError(Exception error);
    }

    /// <summary>
    /// Abstraction over a single observer that
    /// allows signalling the terminal state via safe
    /// method calls.
    /// </summary>
    /// <remarks>Since 0.0.5</remarks>
    public interface ISingleEmitter<in T>
    {
        /// <summary>
        /// Returns true if this emitter has been disposed by
        /// the downstream. Any further calls to 
        /// <see cref="OnSuccess"/> and <see cref="OnError"/>
        /// will be ignored.
        /// </summary>
        /// <returns>True if this emitter has been disposed.</returns>
        bool IsDisposed();

        /// <summary>
        /// Sets a disposable resource to be disposed after the
        /// emitter is terminated or when it gets disposed.
        /// </summary>
        /// <param name="d">The resource to dispose upon termination
        /// or cancellation.</param>
        void SetResource(IDisposable d);

        /// <summary>
        /// Signal a success item to the downstream single observer.
        /// </summary>
        /// <param name="item">The item to signal.</param>
        void OnSuccess(T item);

        /// <summary>
        /// Signal an error to the downstream single observer.
        /// </summary>
        /// <param name="error">The error to signal.</param>
        void OnError(Exception error);
    }
}
