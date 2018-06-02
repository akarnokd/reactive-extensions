using System;
using System.Collections.Generic;
using System.Text;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Base interface for observing signals.<br/>
    /// The protocol is:<br/>
    /// <code>OnSubscribe OnNext* (OnError|OnCompleted)?</code>
    /// </summary>
    /// <typeparam name="T">The element type of the sequence.</typeparam>
    /// <remarks>Since 0.0.17</remarks>
    public interface IObservableSource<out T>
    {
        /// <summary>
        /// Subscribes to this observable source and begins
        /// signaling events until the observer disposes the
        /// IDisposable provided via its OnSubscribe method.
        /// </summary>
        /// <param name="observer">The observer that wants to consume
        /// this observable source.</param>
        void Subscribe(ISignalObserver<T> observer);
    }

    /// <summary>
    /// Base interface for consuming <see cref="IObservableSource{T}"/>s.
    /// The protocol is:<br/>
    /// <code>OnSubscribe OnNext* (OnError|OnCompleted)?</code>
    /// </summary>
    /// <typeparam name="T">The element type of the sequence.</typeparam>
    /// <remarks>Since 0.0.17</remarks>
    public interface ISignalObserver<in T>
    {
        /// <summary>
        /// Called by the upstream <see cref="IObservableSource{T}"/>
        /// before signaling the other event types and allows
        /// canceling the sequence synchronously or asynchronously
        /// from within this observer.
        /// </summary>
        /// <param name="d">The disposable representing the active
        /// connection between an observable source and this signal observer.</param>
        void OnSubscribe(IDisposable d);

        /// <summary>
        /// Called by the upstream <see cref="IObservableSource{T}"/>
        /// when an item becomes ready.
        /// <p>
        /// When the signal observer runs in synchronous fusion mode,
        /// this method should never be called.
        /// When the signal observer runs in asynchronous mode, the
        /// <paramref name="item"/> should be ignored.
        /// </p>
        /// </summary>
        /// <param name="item">The item ready for consumption.</param>
        void OnNext(T item);

        /// <summary>
        /// Called by the upstream <see cref="IObservableSource{T}"/>
        /// when it can no longer produce items due to some error and
        /// the sequence should terminate.
        /// </summary>
        /// <param name="ex">The exception terminating the sequence.</param>
        void OnError(Exception ex);

        /// <summary>
        /// Called by the upstream <see cref="IObservableSource{T}"/>
        /// when no further items can be produced and the sequence
        /// should terminate normally.
        /// </summary>
        void OnCompleted();
    }

    /// <summary>
    /// Abstraction over an <see cref="ISignalObserver{T}"/> that
    /// allows signaling the terminal state via safe
    /// method calls.
    /// </summary>
    /// <remarks>Since 0.0.17</remarks>
    public interface ISignalEmitter<in T>
    {
        /// <summary>
        /// Signal an item to the downstream observer.
        /// </summary>
        /// <param name="item">The item to signal.</param>
        void OnNext(T item);

        /// <summary>
        /// Signal an error and terminate the sequence by
        /// disposing any associated resources set via
        /// <see cref="SetResource(IDisposable)"/>.
        /// </summary>
        /// <param name="ex">The error to signal.</param>
        void OnError(Exception ex);

        /// <summary>
        /// Signal the downstream that no further items will be
        /// emitted and dispose any associated resources set via
        /// <see cref="SetResource(IDisposable)"/>.
        /// </summary>
        void OnCompleted();

        /// <summary>
        /// Sets a new resource to be disposed when the emitter
        /// is terminated or the downstream disposes the sequence.
        /// When called multiple times, the previous resources get
        /// disposed when a new one is set.
        /// </summary>
        /// <param name="d">The resource to set, null to clear any current resource.</param>
        void SetResource(IDisposable d);

        /// <summary>
        /// Returns true if the downstream requested the cancellation
        /// of the sequence.
        /// </summary>
        /// <returns>True if the emitter should stop emitting signals.</returns>
        bool IsDisposed();
    }

    /// <summary>
    /// A basic queue interface for getting out items,
    /// checking for emptyness and clearing the queue.
    /// </summary>
    /// <typeparam name="T">The element type queued.</typeparam>
    /// <remarks>Since 0.0.17</remarks>
    public interface IQueueConsumer<out T>
    {
        /// <summary>
        /// Try getting the next available item from the queue.
        /// </summary>
        /// <param name="success">Set to true if the queue was not empty and
        /// the method returned an item from it.</param>
        /// <returns>The item from the queue if <paramref name="success"/> was true.</returns>
        T TryPoll(out bool success);

        /// <summary>
        /// Check if the queue is empty.
        /// </summary>
        /// <returns>True if the queue is empty.</returns>
        bool IsEmpty();

        /// <summary>
        /// Clear the contents of the queue.
        /// </summary>
        void Clear();
    }

    /// <summary>
    /// A basic queue implementation with production
    /// and consumption options.
    /// </summary>
    /// <typeparam name="T">The element type of the queue.</typeparam>
    /// <remarks>Since 0.0.17</remarks>
    public interface ISimpleQueue<T> : IQueueConsumer<T>
    {
        /// <summary>
        /// Try enqueueing an item in the queue.
        /// </summary>
        /// <param name="item">The item to enqueue.</param>
        /// <returns>True if the operation succeeded, false if the queue is full.</returns>
        bool TryOffer(T item);
    }

    /// <summary>
    /// Represents a fuseable source.
    /// </summary>
    /// <typeparam name="T">The element type</typeparam>
    /// <remarks>Since 0.0.17</remarks>
    public interface IFuseableDisposable<out T> : IQueueConsumer<T>, IDisposable
    {
        /// <summary>
        /// Request a fusion mode from the implementing source operator.
        /// See <see cref="FusionSupport"/> for allowed settings.
        /// </summary>
        /// <param name="mode">The fusion mode requested.</param>
        /// <returns>The established fusion mode: <see cref="FusionSupport.None"/>, <see cref="FusionSupport.Sync"/> or <see cref="FusionSupport.Async"/></returns>
        int RequestFusion(int mode);
    }

    /// <summary>
    /// Constants for the <see cref="IFuseableDisposable{T}.RequestFusion(int)"/>
    /// parameter and return value.
    /// </summary>
    /// <remarks>Since 0.0.17</remarks>
    public static class FusionSupport
    {
        /// <summary>
        /// Indicates the requested fusion mode can't be established and
        /// signals will be emitted via the regular observable source protocol.
        /// </summary>
        public static readonly int None = 0;
        /// <summary>
        /// Either requested as fusion mode or returned as the established fusion
        /// mode. Producers in this mode should never call the OnXXX methods
        /// and consumers polling via <see cref="IQueueConsumer{T}.TryPoll(out bool)"/> have to stop if the queue becomes empty.
        /// </summary>
        public static readonly int Sync = 1;
        /// <summary>
        /// Either requested as fusion mode or returned as the established fusion
        /// mode. Producers should call OnNext with the default value for the sequence
        /// element type T and consumers should ignore this value, polling only via
        /// <see cref="IQueueConsumer{T}.TryPoll(out bool)"/>. The producer has to signal OnError/OnCompleted as normal. 
        /// </summary>
        public static readonly int Async = 2;
        /// <summary>
        /// Combines the sync and async fusion mode requests.
        /// </summary>
        public static readonly int Any = Sync | Async;
        /// <summary>
        /// Request flag indicating the fusion involves an asynchronous boundary
        /// which would break the thread confinement of user callbacks upstream
        /// of the operator.
        /// </summary>
        public static readonly int Boundary = 4;
        /// <summary>
        /// Combines the sync fusion request with the boundary indicator to
        /// request a sync boundary fusion mode.
        /// </summary>
        public static readonly int SyncBoundary = Sync | Boundary;
        /// <summary>
        /// Combines the async fusion request with the boundary indicator to
        /// request an async boundary fusion mode.
        /// </summary>
        public static readonly int AsyncBoundary = Async | Boundary;
        /// <summary>
        /// Combines both the sync and async fusion modes with the barrier indicator
        /// to request any type of boundary-limited fusion mode.
        /// </summary>
        public static readonly int AnyBoundary = Any | Boundary;
    }

    /// <summary>
    /// Indicator interface for a reactive source
    /// indicating it can return a scalar value upon
    /// subscription directly.
    /// </summary>
    /// <typeparam name="T">The scalar value type.</typeparam>
    /// <remarks>Since 0.0.17</remarks>
    public interface IDynamicValue<out T>
    {
        /// <summary>
        /// Returns the scalar value of the source if the success is
        /// set to true, indicates emptiness by returning false and
        /// signaling an error by throwing an exception.
        /// </summary>
        /// <param name="success">If true, the returned value is the outcome, if false, the source should be considered empty.</param>
        /// <returns>The scalar value if success was true.</returns>
        T GetValue(out bool success);
    }

    /// <summary>
    /// An indicator interface for a reactive source
    /// indicating it can return a scalar value (constant) during
    /// flow assembly.
    /// </summary>
    /// <typeparam name="T">The scalar value type.</typeparam>
    /// <remarks>Since 0.0.17</remarks>
    public interface IStaticValue<out T> : IDynamicValue<T>
    {
    }

}
