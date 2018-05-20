using System;
using System.Collections.Generic;
using System.Text;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// A reactive base type that only completes or signals error.
    /// It follows the <code>OnSubscribe (OnError|OnCompleted)?</code> protocol.
    /// </summary>
    /// <remarks>Since 0.0.5</remarks>
    public interface ICompletableSource
    {
        /// <summary>
        /// Subscribes a observer to this completable source.
        /// The implementation should call <see cref="ICompletableObserver.OnSubscribe(IDisposable)"/>
        /// first with a disposable representing the connection/channel between
        /// this completable source and the completable observer.
        /// It is the responsibility of the completable source to dispose
        /// any resource it uses upon disposing this connection disposable or
        /// when the observer is sent an OnError or OnCompleted signal.
        /// </summary>
        /// <param name="observer">The observer that wants to consume this completable.</param>
        void Subscribe(ICompletableObserver observer);
    }

    /// <summary>
    /// A consumer of <see cref="ICompletableSource"/>s which only
    /// complete or signal an error.
    /// It follows the <code>OnSubscribe (OnError|OnCompleted)?</code> protocol.
    /// </summary>
    /// <remarks>Since 0.0.5</remarks>
    public interface ICompletableObserver
    {
        /// <summary>
        /// Sets up the dispose channel between this completable observer and
        /// the completable source. Should be called before any other
        /// OnXXX method.
        /// </summary>
        /// <param name="d">The disposable representing the channel/connection
        /// between the upstream completable source and this completable observer.</param>
        void OnSubscribe(IDisposable d);

        /// <summary>
        /// Signal a failure with an Exception.
        /// When this signal arrives, the upstream completable source
        /// should be considered disposed; there is no need to call
        /// dispose on the connection object.
        /// </summary>
        /// <param name="error">The error to signal.</param>
        void OnError(Exception error);

        /// <summary>
        /// Signal a normal completion.
        /// When this signal arrives, the upstream completable source
        /// should be considered disposed; there is no need to call
        /// dispose on the connection object.
        /// </summary>
        void OnCompleted();
    }

    /// <summary>
    /// Abstraction over a completable observer that
    /// allows signaling the terminal state via safe
    /// method calls.
    /// </summary>
    /// <remarks>Since 0.0.5</remarks>
    public interface ICompletableEmitter
    {
        /// <summary>
        /// Returns true if this emitter has been disposed by
        /// the downstream. Any further calls to <see cref="OnError"/>
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
        /// Signal an error to the downstream completable observer.
        /// </summary>
        /// <param name="error">The error to signal.</param>
        void OnError(Exception error);

        /// <summary>
        /// Signal a completion to the downstream completable observer.
        /// </summary>
        void OnCompleted();
    }
}
