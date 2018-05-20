using System;
using System.Collections.Generic;
using System.Text;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// A reactive base type that only succeeds, completes as empty or signals error.
    /// It follows the <code>OnSubscribe (OnSuccess|OnError|OnCompleted)?</code> protocol.
    /// </summary>
    /// <typeparam name="T">The element type of this reactive source.</typeparam>
    /// <remarks>Since 0.0.5</remarks>
    public interface IMaybeSource<out T>
    {
        /// <summary>
        /// Subscribes a observer to this maybe source.
        /// The implementation should call <see cref="IMaybeObserver{T}.OnSubscribe(IDisposable)"/>
        /// first with a disposable representing the connection/channel between
        /// this maybe source and the maybe observer.
        /// It is the responsibility of the maybe source to dispose
        /// any resource it uses upon disposing this connection disposable or
        /// when the observer is sent an OnSuccess, OnError or OnCompleted signal.
        /// </summary>
        /// <param name="observer">The observer that wants to consume this maybe.</param>
        void Subscribe(IMaybeObserver<T> observer);
    }

    /// <summary>
    /// A consumer of <see cref="IMaybeSource{T}"/>s which only
    /// succeeds with one item, completes as empty or signals an error.
    /// It follows the <code>OnSubscribe (OnSuccess|OnError|OnCompleted)?</code> protocol.
    /// </summary>
    /// <remarks>Since 0.0.5</remarks>
    public interface IMaybeObserver<in T>
    {
        /// <summary>
        /// Sets up the dispose channel between this maybe observer and
        /// the maybe source. Should be called before any other
        /// OnXXX method.
        /// </summary>
        /// <param name="d">The disposable representing the channel/connection
        /// between the upstream maybe source and this maybe observer.</param>
        void OnSubscribe(IDisposable d);

        /// <summary>
        /// Signal the only success item.
        /// When this signal arrives, the upstream maybe source
        /// should be considered disposed; there is no need to call
        /// dispose on the connection object.
        /// </summary>
        /// <param name="item">The success item to signal.</param>
        void OnSuccess(T item);

        /// <summary>
        /// Signal a failure with an Exception.
        /// When this signal arrives, the upstream maybe source
        /// should be considered disposed; there is no need to call
        /// dispose on the connection object.
        /// </summary>
        /// <param name="error">The error to signal.</param>
        void OnError(Exception error);

        /// <summary>
        /// Signal a normal completion.
        /// When this signal arrives, the upstream maybe source
        /// should be considered disposed; there is no need to call
        /// dispose on the connection object.
        /// </summary>
        void OnCompleted();
    }

    /// <summary>
    /// Abstraction over a maybe observer that
    /// allows signaling the terminal state via safe
    /// method calls.
    /// </summary>
    /// <remarks>Since 0.0.5</remarks>
    public interface IMaybeEmitter<in T>
    {
        /// <summary>
        /// Returns true if this emitter has been disposed by
        /// the downstream. Any further calls to 
        /// <see cref="OnSuccess"/>, <see cref="OnError"/>
        /// and <see cref="OnCompleted"/> will be ignored.
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
        /// Signal a success item to the downstream maybe observer.
        /// </summary>
        /// <param name="item">The item to signal.</param>
        void OnSuccess(T item);

        /// <summary>
        /// Signal an error to the downstream maybe observer.
        /// </summary>
        /// <param name="error">The error to signal.</param>
        void OnError(Exception error);

        /// <summary>
        /// Signal a completion to the downstream maybe observer.
        /// </summary>
        void OnCompleted();
    }
}
